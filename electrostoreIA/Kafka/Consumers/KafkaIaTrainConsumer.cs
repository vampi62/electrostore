using System.Text.Json;
using Confluent.Kafka;
using ElectrostoreIA.Dto;
using ElectrostoreIA.Enums;
using ElectrostoreIA.Kafka.Producer;
using ElectrostoreIA.Kafka.Messages;
using ElectrostoreIA.Services.ModelTrainerService;

namespace ElectrostoreIA.Kafka.Consumers;

public class KafkaIaTrainConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly IModelTrainerService _trainerService;
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaIaTrainConsumer> _logger;

    private readonly SemaphoreSlim _trainingSemaphore = new(1, 1);

    public KafkaIaTrainConsumer(
        IModelTrainerService trainerService,
        IKafkaProducerService kafkaProducer,
        IConfiguration configuration,
        ILogger<KafkaIaTrainConsumer> logger)
    {
        _trainerService = trainerService;
        _kafkaProducer = kafkaProducer;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrapServers = _configuration.GetValue<string>("Kafka:BootstrapServers") ?? "kafka:9092";
        var groupId = _configuration.GetValue<string>("Kafka:ConsumerGroupId") ?? "ia-service";

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
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
        consumer.Subscribe("ia-requests");
        _logger.LogInformation("KafkaIaTrainConsumer started (group={Group}, servers={Servers})", groupId, bootstrapServers);
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result = null;
                try
                {
                    result = consumer.Consume(TimeSpan.FromSeconds(2));
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming Kafka message: {Error}", ex.Error.Reason);
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
                TrainRequestMessage? msg;
                try
                {
                    msg = JsonSerializer.Deserialize<TrainRequestMessage>(result.Message.Value, JsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Invalid Kafka message (JSON) — offset {Offset}", result.Offset);
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
            _logger.LogInformation("KafkaIaTrainConsumer stopped");
        }
    }

    private async Task DispatchAsync(TrainRequestMessage msg, CancellationToken ct)
    {
        if (msg is null || (msg.action != "train_requested" && msg.action != "ia_deleted"))
        {
            _logger.LogWarning("Unknown action in message: {Action}", msg?.action);
            return;
        }

        // ---- ia_deleted ----
        if (msg.action == "ia_deleted")
        {
            _logger.LogInformation("Received delete request for IA {Id}", msg.id_ia);
            try
            {
                await _trainerService.DeleteModelFilesAsync(msg.id_ia, ct);
                _logger.LogInformation("Model files deleted for IA {Id}", msg.id_ia);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete model files for IA {Id}", msg.id_ia);
            }
            return;
        }

        // ---- train_requested ----
        int idIa = msg.id_ia;
        int requestedBy = msg.requested_by;
        _logger.LogInformation("Received training request for IA {Id} from user {UserId}", idIa, requestedBy);
        _trainerService.SetTrainingProgressMap(idIa, new TrainingProgress
        {
            Status = TrainingStatus.InWaiting,
            Message = "Awaiting training slot",
            RequestedBy = requestedBy
        });

        // Await the semaphore to ensure only one training runs at a time
        await _trainingSemaphore.WaitAsync(ct);

        // ia-status: training started
        try
        {
            var iaStatus = new IaStatusMessage
            {
                action = "training_started",
                id_ia = idIa,
                status = "in_progress",
                message = "Training has started",
                requested_by = requestedBy,
                accuracy = 0f,
                val_accuracy = 0f,
                loss = 0f,
                val_loss = 0f,
                epoch = 0
            };
            await _kafkaProducer.PublishAsync(
                "ia-status",
                idIa.ToString(),
                JsonSerializer.Serialize(iaStatus),
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish ia-status (started) for IA {Id}", idIa);
        }

        bool success = false;
        string resultMessage;
        try
        {
            success = await _trainerService.StartAndWaitAsync(idIa, requestedBy, ct);
            resultMessage = _trainerService.GetTrainingStatus(idIa) is not null
                ? _trainerService.GetTrainingStatus(idIa)!.Message
                : (success ? "Training completed successfully" : "Training failed with unknown error");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Training for IA {Id} was cancelled", idIa);
            resultMessage = "Training was cancelled";
            _trainingSemaphore.Release();
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during training for IA {Id}: {Message}", idIa, ex.Message);
            resultMessage = ex.Message;
        }
        finally
        {
            _trainingSemaphore.Release();
        }

        // ia-status: training result
        try
        {
            var finalStatus = _trainerService.GetTrainingStatus(idIa);
            await _kafkaProducer.PublishAsync(
                "ia-status",
                idIa.ToString(),
                JsonSerializer.Serialize(new
                {
                    action = success ? "training_completed" : "training_failed",
                    id_ia = idIa,
                    status = success ? "completed" : "error",
                    message = resultMessage,
                    requested_by = requestedBy,
                    finished_at = DateTime.UtcNow,
                    accuracy = finalStatus?.Accuracy ?? 0f,
                    val_accuracy = finalStatus?.ValAccuracy ?? 0f,
                    loss = finalStatus?.Loss ?? 0f,
                    val_loss = finalStatus?.ValLoss ?? 0f,
                    epoch = finalStatus?.Epoch ?? 0
                }),
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish ia-status (result) for IA {Id}", idIa);
        }
        _logger.LogInformation("Finished processing training request for IA {Id} with result: {Result}", idIa, success ? "Success" : "Failure");
    }
}
