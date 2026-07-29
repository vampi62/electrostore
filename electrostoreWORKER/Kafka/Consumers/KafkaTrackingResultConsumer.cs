using System.Text.Json;
using Confluent.Kafka;
using ElectrostoreWORKER.Grpc;
using ElectrostoreWORKER.Kafka.Messages;

namespace ElectrostoreWORKER.Kafka.Consumers;

public class KafkaTrackingResultConsumer : BackgroundService
{
    private const string Topic = "tracking-result";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly CommandsGrpc.CommandsGrpcClient _commandsClient;
    private readonly IConfiguration                  _configuration;
    private readonly ILogger<KafkaTrackingResultConsumer> _logger;

    public KafkaTrackingResultConsumer(
        CommandsGrpc.CommandsGrpcClient commandsClient,
        IConfiguration configuration,
        ILogger<KafkaTrackingResultConsumer> logger)
    {
        _commandsClient = commandsClient;
        _configuration  = configuration;
        _logger         = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "kafka:9092";
        var groupId          = _configuration["Kafka:ConsumerGroupId"]  ?? "worker-service";

        var config = new ConsumerConfig
        {
            BootstrapServers    = bootstrapServers,
            GroupId             = groupId,
            AutoOffsetReset     = AutoOffsetReset.Earliest,
            EnableAutoCommit    = false,
            EnablePartitionEof  = true,
            SessionTimeoutMs    = 60_000,
            HeartbeatIntervalMs = 15_000,
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) =>
                _logger.LogError(
                    "[Kafka] Broker error | Code={Code} | Reason={Reason} | Fatal={Fatal}",
                    e.Code, e.Reason, e.IsFatal))
            .SetPartitionsAssignedHandler((_, parts) =>
                _logger.LogInformation(
                    "[Kafka] Partitions assigned → {Parts}",
                    string.Join(", ", parts.Select(p => $"{p.Topic}[{p.Partition}]"))))
            .SetPartitionsRevokedHandler((_, parts) =>
                _logger.LogWarning(
                    "[Kafka] Partitions revoked → {Parts}",
                    string.Join(", ", parts.Select(p => $"{p.Topic}[{p.Partition}]"))))
            .Build();

        consumer.Subscribe(Topic);
        _logger.LogInformation(
            "KafkaTrackingResultConsumer started (group={Group}, servers={Servers})",
            groupId, bootstrapServers);

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

    private TrackingResultMessage? DeserializeMessage(ConsumeResult<string, string> result)
    {
        try
        {
            return JsonSerializer.Deserialize<TrackingResultMessage>(result.Message.Value, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid Kafka message (JSON) - offset {Offset}", result.Offset);
            return null;
        }
    }

    private async Task<bool> DispatchAsync(TrackingResultMessage msg, CancellationToken ct)
    {
        if (!msg.success)
        {
            _logger.LogWarning(
                "Track17 result: action={Action} tracking={Num} carrier={Carrier} - échec (error_code={Err}).",
                msg.action, msg.tracking_number, msg.carrier, msg.error_code);
            return false;
        }

        switch (msg.action)
        {
            case "register":
            case "stoptrack":
            case "retrack":
            case "deletetrack":
                var reply = await _commandsClient.UpdateCommandStatusAsync(
                    new UpdateCommandStatusRequest
                    {
                        KeyCarrier     = msg.carrier,
                        TrackingNumber = msg.tracking_number,
                        Action         = msg.action,
                        Success        = msg.success,
                        ErrorCode      = msg.error_code ?? 0,
                    },
                    cancellationToken: ct);

                if (reply.Success)
                    _logger.LogInformation(
                        "Track17 result: action={Action} tracking={Num} carrier={Carrier} - commands updated.",
                        msg.action, msg.tracking_number, msg.carrier);
                else
                    _logger.LogWarning(
                        "Track17 result: action={Action} tracking={Num} carrier={Carrier} - gRPC update rejected.",
                        msg.action, msg.tracking_number, msg.carrier);
                break;

            // changecarrier, changeinfo, push — no DB status update (push tracking info comes from webhook)
            default:
                _logger.LogInformation(
                    "Track17 result: action={Action} tracking={Num} carrier={Carrier} - acknowledged.",
                    msg.action, msg.tracking_number, msg.carrier);
                break;
        }
        return true;
    }
}
