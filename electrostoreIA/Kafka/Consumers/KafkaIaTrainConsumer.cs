using System.Text.Json;
using Confluent.Kafka;
using ElectrostoreIA.Dto;
using ElectrostoreIA.Enums;
using ElectrostoreIA.Services.ModelTrainerService;
using ElectrostoreIA.Kafka.Producer;

namespace ElectrostoreIA.Kafka.Consumers;

public class KafkaIaTrainConsumer : BackgroundService
{
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
            MaxPollIntervalMs = 86_400_000, // 24 h
            SessionTimeoutMs = 60_000,
        };
        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe("ia-requests");
        _logger.LogInformation("KafkaIaTrainConsumer started (group={Group}, servers={Servers})", groupId, bootstrapServers);
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
                    _logger.LogError(ex, "Error consuming Kafka message: {Error}", ex.Error.Reason);
                    continue;
                }
                if (result?.Message?.Value is null)
                {
                    continue;
                }
                TrainRequestMessage? msg;
                try
                {
                    msg = JsonSerializer.Deserialize<TrainRequestMessage>(result.Message.Value);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Invalid message format: {Value}", result.Message.Value);
                    consumer.Commit(result);
                    continue;
                }
                if (msg is null || (msg.action != "train_requested" && msg.action != "ia_deleted"))
                {
                    consumer.Commit(result);
                    continue;
                }

                // ---- ia_deleted ----
                if (msg.action == "ia_deleted")
                {
                    _logger.LogInformation("Received delete request for IA {Id}", msg.id_ia);
                    try
                    {
                        await _trainerService.DeleteModelFilesAsync(msg.id_ia, stoppingToken);
                        _logger.LogInformation("Model files deleted for IA {Id}", msg.id_ia);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to delete model files for IA {Id}", msg.id_ia);
                    }
                    consumer.Commit(result);
                    continue;
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
                await _trainingSemaphore.WaitAsync(stoppingToken);

                // ia-status: training started
                try
                {
                    await _kafkaProducer.PublishAsync(
                        "ia-status",
                        idIa.ToString(),
                        JsonSerializer.Serialize(new { action = "training_started", id_ia = idIa, status = "in_progress", requested_by = requestedBy, started_at = DateTime.UtcNow }),
                        stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish ia-status (started) for IA {Id}", idIa);
                }

                bool success = false;
                string resultMessage;
                try
                {
                    success = await _trainerService.StartAndWaitAsync(idIa, requestedBy, stoppingToken);
                    resultMessage = _trainerService.GetTrainingStatus(idIa) is not null
                        ? _trainerService.GetTrainingStatus(idIa)!.Message
                        : (success ? "Training completed successfully" : "Training failed with unknown error");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Training for IA {Id} was cancelled", idIa);
                    resultMessage = "Training was cancelled";
                    _trainingSemaphore.Release();
                    break;
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
                        stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish ia-status (result) for IA {Id}", idIa);
                }
                consumer.Commit(result);
                _logger.LogInformation("Finished processing training request for IA {Id} with result: {Result}", idIa, success ? "Success" : "Failure");
            }
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("KafkaIaTrainConsumer stopped");
        }
    }
}
