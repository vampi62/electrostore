using Confluent.Kafka;
using ElectrostoreWORKER.DTO;
using ElectrostoreWORKER.Grpc;
using System.Text.Json;

namespace ElectrostoreWORKER.Kafka.Consumers;

public class KafkaIaStatusConsumer : BackgroundService
{
    private const string Topic = "ia-status";
    private readonly WORKERToAPIGrpc.WORKERToAPIGrpcClient _dataService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaIaStatusConsumer> _logger;

    public KafkaIaStatusConsumer(
        WORKERToAPIGrpc.WORKERToAPIGrpcClient dataService,
        IConfiguration configuration,
        ILogger<KafkaIaStatusConsumer> logger)
    {
        _dataService   = dataService;
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

                IaStatusMessage? msg;
                try
                {
                    msg = JsonSerializer.Deserialize<IaStatusMessage>(result.Message.Value);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Invalid ia-status message: {Payload}", result.Message.Value);
                    consumer.Commit(result);
                    continue;
                }

                if (msg is null)
                {
                    consumer.Commit(result);
                    continue;
                }

                try
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
                        cancellationToken: stoppingToken);

                    if (reply.Success)
                    {
                        _logger.LogInformation(
                            "IA #{Id} status updated: action={Action}", msg.id_ia, msg.action);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "API: IA #{Id} status update rejected (action={Action}).", msg.id_ia, msg.action);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error forwarding ia-status (id_ia={Id}, action={Action}).",
                        msg.id_ia, msg.action);
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
            _logger.LogInformation("KafkaIaStatusConsumer stopped");
        }
    }
}
