using Confluent.Kafka;
using Docker.DotNet;
using Docker.DotNet.Models;
using ElectrostoreWORKER.Kafka.Messages;
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

    private readonly DockerClient _dockerClient;
    private readonly string _mosquittoContainerName;
    private const string PasswdFilePath = "/mosquitto/config/mosquitto.passwd";

    public KafkaMqttUserConsumer(
        IConfiguration configuration,
        ILogger<KafkaMqttUserConsumer> logger)
    {
        _configuration = configuration;
        _logger        = logger;
        _dockerClient  = new DockerClientConfiguration().CreateClient();
        _mosquittoContainerName = configuration.GetSection("MQTT:ContainerName").Value ?? "electrostore-mqtt";
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

        using var consumer = new ConsumerBuilder<string, string>(config)
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
            _logger.LogInformation("KafkaIaStatusConsumer stopped");
        }
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
        var dispatched = await DispatchAsync(msg, ct);
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

    private async Task<bool> ExecuteCommandInMosquittoAsync(string command, CancellationToken ct)
    {
        var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true }, ct);
        var mosquittoContainer = containers.FirstOrDefault(c => c.Names.Any(n => n.TrimStart('/').Equals(_mosquittoContainerName, StringComparison.OrdinalIgnoreCase)));
        if (mosquittoContainer is null)
        {
            _logger.LogError("Mosquitto container not found. Cannot execute command.");
            return false;
        }
        try
        {
            var execCreateResponse = await _dockerClient.Exec.ExecCreateContainerAsync(mosquittoContainer.ID, new ContainerExecCreateParameters
            {
                AttachStdout = true,
                AttachStderr = true,
                Cmd = ["sh", "-c", command]
            }, ct);
            var execStartResponse = await _dockerClient.Exec.StartContainerExecAsync(execCreateResponse.ID, ct);
            return execStartResponse?.StatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute command in Mosquitto container: {Message}", ex.Message);
            return false;
        }
    }
}
