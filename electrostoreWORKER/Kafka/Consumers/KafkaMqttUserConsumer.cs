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
    private string _mosquittoContainerName;
    private const string PasswdFilePath = "/mosquitto/config/mosquitto.passwd";

    public KafkaMqttUserConsumer(
        IConfiguration configuration,
        ILogger<KafkaMqttUserConsumer> logger)
    {
        _configuration = configuration;
        _logger        = logger;
        _dockerClient  = new DockerClientConfiguration().CreateClient();
        _mosquittoContainerName = configuration.GetSection("Mqtt:ContainerName").Value ?? "electrostore-mqtt";
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
                ConsumeResult<string, string>? result = null;
                try
                {
                    result = await Task.Run(() => consumer.Consume(stoppingToken), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka error: {Reason}", ex.Error.Reason);
                    continue;
                }
                if (result is null || result.IsPartitionEOF)
                {
                    continue;
                }
                if (result?.Message?.Value is null)
                {
                    continue;
                }
                MqttUserMessage? msg;
                try
                {
                    msg = JsonSerializer.Deserialize<MqttUserMessage>(result.Message.Value, JsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Invalid Kafka message (JSON) - offset {Offset}", result.Offset);
                    consumer.Commit(result);
                    continue;
                }
                if (msg is null)
                {
                    consumer.Commit(result);
                    continue;
                }
                var dispatched = false;
                try
                {
                    await DispatchAsync(msg, stoppingToken);
                    dispatched = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error dispatching message for offset {Offset}: {Message}", result.Offset, ex.Message);
                }
                if (dispatched)
                {
                    consumer.Commit(result);
                }
            }
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("KafkaMqttUserConsumer stopped");
        }
    }

    private async Task DispatchAsync(MqttUserMessage msg, CancellationToken ct)
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
                return;
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
    }

    private async Task ExecuteCommandInMosquittoAsync(string command, CancellationToken ct)
    {
        var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true }, ct);
        var mosquittoContainer = containers.FirstOrDefault(c => c.Names.Any(n => n.TrimStart('/').Equals(_mosquittoContainerName, StringComparison.OrdinalIgnoreCase)));
        if (mosquittoContainer is null)
        {
            _logger.LogError("Mosquitto container not found. Cannot execute command.");
            return;
        }
        try
        {
            var execCreateResponse = await _dockerClient.Exec.ExecCreateContainerAsync(mosquittoContainer.ID, new ContainerExecCreateParameters
            {
                AttachStdout = true,
                AttachStderr = true,
                Cmd = ["sh", "-c", command]
            }, ct);
            await _dockerClient.Exec.StartContainerExecAsync(execCreateResponse.ID, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute command in Mosquitto container: {Message}", ex.Message);
        }
    }
}
