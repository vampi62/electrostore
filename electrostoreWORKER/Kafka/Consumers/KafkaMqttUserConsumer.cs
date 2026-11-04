using Confluent.Kafka;
using Docker.DotNet;
using ElectrostoreWORKER.Kafka.Messages;
using ElectrostoreWORKER.Mqtt;
using System.Text.Json;

namespace ElectrostoreWORKER.Kafka.Consumers;

public class KafkaMqttUserConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private const string Topic = "mqtt-user-events";
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaMqttUserConsumer> _logger;

    // null when MQTT user management is disabled (neither a Docker socket nor a Kubernetes cluster is available).
    private readonly IMosquittoCommandRunner? _runner;
    private const string PasswdFilePath = "/mosquitto/config/mosquitto.passwd";

    public KafkaMqttUserConsumer(
        IConfiguration configuration,
        ILogger<KafkaMqttUserConsumer> logger)
        : this(configuration, logger, MosquittoCommandRunnerFactory.Create(configuration, logger))
    {
    }

    // Enables injecting a mockable IDockerClient from unit tests, since Docker.DotNet's
    // real client isn't otherwise reachable without a live Docker daemon.
    internal KafkaMqttUserConsumer(
        IConfiguration configuration,
        ILogger<KafkaMqttUserConsumer> logger,
        IDockerClient dockerClient)
        : this(configuration, logger, new DockerMosquittoCommandRunner(
            dockerClient,
            configuration.GetSection("MQTT:ContainerName").Value ?? "electrostore-mqtt",
            logger))
    {
    }

    internal KafkaMqttUserConsumer(
        IConfiguration configuration,
        ILogger<KafkaMqttUserConsumer> logger,
        IMosquittoCommandRunner? runner)
    {
        _configuration = configuration;
        _logger        = logger;
        _runner        = runner;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrapServers = _configuration.GetSection("Kafka:BootstrapServers").Value ?? "kafka:9092";
        var groupId = _configuration.GetSection("Kafka:ConsumerGroupId").Value ?? "worker-service";

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId          = groupId,
            AutoOffsetReset  = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnablePartitionEof  = true,
            SessionTimeoutMs = 60_000,
            HeartbeatIntervalMs = 15_000,
        };

        using var consumer = BuildConsumer(config);
        consumer.Subscribe(Topic);

        _logger.LogInformation(
            "KafkaMqttUserConsumer started (group={Group}, servers={Servers})", groupId, bootstrapServers);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = await ConsumeMessageAsync(consumer, stoppingToken);
                if (result is null)
                {
                    continue;
                }
                await ProcessMessageAsync(consumer, result, stoppingToken);
            }
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("KafkaMqttUserConsumer stopped.");
        }
    }

    // Virtual so unit tests can substitute a mocked IConsumer instead of building a real
    // Kafka connection.
    protected virtual IConsumer<string, string> BuildConsumer(ConsumerConfig config)
    {
        return new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) =>
                _logger.LogError(
                    "[Kafka] Broker error | Code: {Code} | Reason: {Reason} | Fatal: {Fatal}",
                    e.Code, e.Reason, e.IsFatal))
            .SetPartitionsAssignedHandler((_, partitions) =>
                _logger.LogInformation(
                    "[Kafka] Partitions assigned → {Parts}",
                    string.Join(", ", partitions.Select(p => $"{p.Topic}[{p.Partition}]"))))
            .SetPartitionsRevokedHandler((_, partitions) =>
                _logger.LogWarning(
                    "[Kafka] Partitions revoked → {Parts}",
                    string.Join(", ", partitions.Select(p => $"{p.Topic}[{p.Partition}]"))))
            .Build();
    }

    private async Task<ConsumeResult<string, string>?> ConsumeMessageAsync(
        IConsumer<string, string> consumer, 
        CancellationToken ct)
    {
        try
        {
            return await Task.Run(() => consumer.Consume(ct), ct);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (ConsumeException ex)
        {
            _logger.LogError(ex, "Kafka error: {Reason}", ex.Error.Reason);
            return null;
        }
    }

    private async Task ProcessMessageAsync(
        IConsumer<string, string> consumer,
        ConsumeResult<string, string> result,
        CancellationToken ct)
    {
        if (result.IsPartitionEOF || result.Message?.Value is null)
        {
            return;
        }
        var msg = DeserializeMessage(result);
        if (msg is null)
        {
            consumer.Commit(result);
            return;
        }
        var dispatched = false;
        try
        {
            dispatched = await DispatchAsync(msg, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching message for offset {Offset}", result.Offset);
        }
        if (dispatched)
        {
            consumer.Commit(result);
        } else
        {
            _logger.LogWarning("Message for offset {Offset} was not dispatched.", result.Offset);
        }
    }

    private MqttUserMessage? DeserializeMessage(ConsumeResult<string, string> result)
    {
        try
        {
            return JsonSerializer.Deserialize<MqttUserMessage>(result.Message.Value, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid Kafka message (JSON) - offset {Offset}", result.Offset);
            return null;
        }
    }

    private async Task<bool> DispatchAsync(MqttUserMessage msg, CancellationToken ct)
    {
        if (_runner is null)
        {
            _logger.LogWarning("MQTT user management is disabled: ignoring event for user {User}.", msg.user);
            return true;
        }
        if (msg.delete ?? false)
        {
            _logger.LogInformation("Deleting MQTT user: {User}", msg.user);
            await ExecuteCommandInMosquittoAsync($"mosquitto_passwd -D {PasswdFilePath} {msg.user}", ct);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(msg.user) || string.IsNullOrWhiteSpace(msg.password))
            {
                _logger.LogWarning("Invalid MQTT user message: missing user or password");
                return false;
            }
            if (!string.IsNullOrWhiteSpace(msg.old_user) && msg.old_user != msg.user)
            {
                _logger.LogInformation("Renaming MQTT user: {OldUser} → {NewUser}", msg.old_user, msg.user);
                await ExecuteCommandInMosquittoAsync($"mosquitto_passwd -D {PasswdFilePath} {msg.old_user}", ct);
            }
            _logger.LogInformation("Adding/updating MQTT user: {User}", msg.user);
            await ExecuteCommandInMosquittoAsync($"mosquitto_passwd -b {PasswdFilePath} {msg.user} {msg.password}", ct);
        }
        _logger.LogInformation("Reloading Mosquitto configuration");
        await ExecuteCommandInMosquittoAsync("kill -HUP 1", ct);
        return true;
    }

    private Task<bool> ExecuteCommandInMosquittoAsync(string command, CancellationToken ct)
    {
        return _runner!.ExecuteAsync(command, ct);
    }
}
