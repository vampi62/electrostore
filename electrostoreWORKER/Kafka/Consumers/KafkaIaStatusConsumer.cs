using Confluent.Kafka;
using ElectrostoreWORKER.Grpc;
using ElectrostoreWORKER.Kafka.Messages;
using System.Text.Json;

namespace ElectrostoreWORKER.Kafka.Consumers;

public class KafkaIaStatusConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private const string Topic = "ia-status";
    private readonly IaTrainingGrpc.IaTrainingGrpcClient _dataService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaIaStatusConsumer> _logger;

    public KafkaIaStatusConsumer(
        IaTrainingGrpc.IaTrainingGrpcClient dataService,
        IConfiguration configuration,
        ILogger<KafkaIaStatusConsumer> logger)
    {
        _dataService   = dataService;
        _configuration = configuration;
        _logger        = logger;
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
            "KafkaIaStatusConsumer started (group={Group}, servers={Servers})", groupId, bootstrapServers);

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
                IaStatusMessage? msg;
                try
                {
                    msg = JsonSerializer.Deserialize<IaStatusMessage>(result.Message.Value, JsonOptions);
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
            _logger.LogInformation("KafkaIaStatusConsumer stopped");
        }
    }

    private async Task DispatchAsync(IaStatusMessage msg, CancellationToken ct)
    {
        var reply = await _dataService.UpdateIaStatusAsync(
            new UpdateIaStatusRequest
            {
                IdIa       = msg.id_ia,
                Action     = msg.action,
                RequestedBy = msg.requested_by,
                Message    = msg.message,
                Accuracy   = msg.accuracy,
                ValAccuracy = msg.val_accuracy,
                Loss       = msg.loss,
                ValLoss    = msg.val_loss,
                Epoch      = msg.epoch,
            },
            cancellationToken: ct);
        if (reply.Success)
        {
            _logger.LogInformation("IA #{Id} status updated: action={Action}", msg.id_ia, msg.action);
        }
        else
        {
            _logger.LogWarning("API: IA #{Id} status update rejected (action={Action}).", msg.id_ia, msg.action);
        }
    }
}
