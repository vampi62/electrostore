using System.Text.Json;
using Confluent.Kafka;
using ElectrostoreWORKER.DTO;
using ElectrostoreWORKER.Grpc;
using Grpc.Core;

namespace ElectrostoreWORKER.Kafka.Consumers;

public class KafkaCronConsumer : BackgroundService
{
    private const string TopicParcel = "cron-parcel-tracking";

    private readonly CommandsGrpc.CommandsGrpcClient _apiClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaCronConsumer> _logger;

    public KafkaCronConsumer(
        CommandsGrpc.CommandsGrpcClient apiClient,
        IConfiguration configuration,
        ILogger<KafkaCronConsumer> logger)
    {
        _apiClient   = apiClient;
        _configuration = configuration;
        _logger        = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "kafka:9092";
        var groupId          = _configuration["Kafka:ConsumerGroupId"]  ?? "worker-service";

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId          = groupId,
            AutoOffsetReset  = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(new[] { TopicParcel });

        _logger.LogInformation(
            "KafkaCronConsumer started (group={Group}, servers={Servers})", groupId, bootstrapServers);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result = null;
                try
                {
                    result = consumer.Consume(stoppingToken);
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

                if (result?.Message?.Value is null)
                    continue;

                try
                {
                    await DispatchAsync(result.Topic, result.Message.Value, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message (topic={Topic}).", result.Topic);
                }
                finally
                {
                    consumer.Commit(result);
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task DispatchAsync(string topic, string payload, CancellationToken ct)
    {
        switch (topic)
        {
            case TopicParcel:
                await HandleParcelTrackingAsync(payload, ct);
                break;

            default:
                _logger.LogWarning("Unknown topic: {Topic}", topic);
                break;
        }
    }

    private async Task HandleParcelTrackingAsync(string payload, CancellationToken ct)
    {
        ParcelTrackingMessage? msg;
        try
        {
            msg = JsonSerializer.Deserialize<ParcelTrackingMessage>(payload);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid parcel-tracking message: {Payload}", payload);
            return;
        }

        if (msg is null || msg.id_command == 0)
        {
            _logger.LogWarning("Parcel-tracking message ignored (id_command missing).");
            return;
        }

        var newStatus = MapTrackingStatusToCommand(msg.tracking_status, msg.is_delivered);

        var eventAt = DateTime.TryParse(msg.date_livraison, null,
            System.Globalization.DateTimeStyles.RoundtripKind, out var parsedDate)
            ? parsedDate
            : DateTime.UtcNow;

        await Task.WhenAll(
            UpdateCommandStatusAsync(
                msg.id_command, newStatus, msg.date_livraison, ct),
            AddCommandHistoryAsync(
                msg.id_command,
                newStatus,
                msg.tracking_number,
                msg.carrier,
                msg.tracking_event,
                eventAt,
                ct));
    }

    private static string MapTrackingStatusToCommand(string trackingStatus, bool isDelivered)
    {
        if (isDelivered) return "delivered";
        return trackingStatus switch
        {
            "in_transit"  => "shipped",
            "out_for_delivery" => "out_for_delivery",
            "exception"   => "delivery_exception",
            _ => trackingStatus,
        };
    }

    private async Task UpdateCommandStatusAsync(
        int idCommand, string status, string dateLivraison, CancellationToken ct = default)
    {
        try
        {
            var reply = await _apiClient.UpdateCommandStatusAsync(
                new UpdateCommandStatusRequest
                {
                    IdCommand = idCommand,
                    StatusCommand = status,
                    DateLivraison = dateLivraison,
                },
                cancellationToken: ct);

            if (reply.Success)
                _logger.LogInformation(
                    "Command #{Id} updated: status={Status}", idCommand, status);
            else
                _logger.LogWarning(
                    "API: command #{Id} status update rejected.", idCommand);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while updating command #{Id} status.", idCommand);
        }
    }

    private async Task AddCommandHistoryAsync(
        int idCommand,
        string status,
        string? trackingNumber,
        string? carrier,
        string? trackingEvent,
        DateTime eventAt,
        CancellationToken ct = default)
    {
        try
        {
            var reply = await _apiClient.AddCommandHistoryAsync(
                new AddCommandHistoryRequest
                {
                    IdCommand      = idCommand,
                    Status         = status,
                    TrackingNumber = trackingNumber ?? string.Empty,
                    Carrier        = carrier        ?? string.Empty,
                    TrackingEvent  = trackingEvent  ?? string.Empty,
                    EventAt        = eventAt.ToString("O"),
                },
                cancellationToken: ct);

            if (reply.Success)
                _logger.LogInformation(
                    "CommandHistory #{HId} added for command #{CId} (status={Status}).",
                    reply.IdCommandHistory, idCommand, status);
            else
                _logger.LogWarning("API: AddCommandHistory rejected for command #{Id}.", idCommand);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while adding command history for command #{Id}.", idCommand);
        }
    }
}
