using ElectrostoreAPI.Kafka.Producer;
using ElectrostoreAPI.Services.ConfigService;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ElectrostoreAPI.Grpc.Services;

public class ElectrostoreWORKERToApiGrpcService : WORKERToAPIGrpc.WORKERToAPIGrpcBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ElectrostoreWORKERToApiGrpcService> _logger;
    private readonly IConfigService _configService;
    private readonly IKafkaProducerService _kafkaProducer;

    public ElectrostoreWORKERToApiGrpcService(
        ApplicationDbContext context,
        ILogger<ElectrostoreWORKERToApiGrpcService> logger,
        IConfigService configService,
        IKafkaProducerService kafkaProducer)
    {
        _context = context;
        _logger = logger;
        _configService = configService;
        _kafkaProducer = kafkaProducer;
    }

    public override async Task<UpdateStoreMqttStatusReply> UpdateStoreMqttStatus(
        UpdateStoreMqttStatusRequest request, ServerCallContext context)
    {
        var stores = await _context.Stores
            .Where(s => s.mqtt_name_store == request.MqttNameStore)
            .ToListAsync(context.CancellationToken);

        if (stores.Count == 0)
        {
            _logger.LogWarning(
                "UpdateStoreMqttStatus: no store found with mqtt_name_store={Name}.", request.MqttNameStore);
            return new UpdateStoreMqttStatusReply { Success = false, StoreCount = 0 };
        }

        var now = DateTime.UtcNow;
        foreach (var store in stores)
        {
            store.is_mqtt_connected_store = request.IsMqttConnected;
            store.mqtt_last_seen_store    = now;
        }

        await _context.SaveChangesAsync(context.CancellationToken);
        return new UpdateStoreMqttStatusReply { Success = true, StoreCount = stores.Count };
    }

    public override Task<WORKERGetConfigReply> GetConfig(
        WORKERGetConfigRequest request, ServerCallContext context)
    {
        return Task.FromResult(new WORKERGetConfigReply { DemoMode = _configService.GetDemoMode() });
    }

    public override async Task<UpdateIaStatusReply> UpdateIaStatus(
        UpdateIaStatusRequest request, ServerCallContext context)
    {
        var ia = await _context.IA.FindAsync(
            new object[] { request.IdIa }, context.CancellationToken);

        if (ia is null)
        {
            _logger.LogWarning("UpdateIaStatus: IA #{Id} not found.", request.IdIa);
            return new UpdateIaStatusReply { Success = false };
        }

        // Update trained_ia flag based on the action
        if (request.Action == "training_completed")
        {
            ia.trained_ia = true;
            ia.date_training_ia = DateTime.UtcNow;
            await _context.SaveChangesAsync(context.CancellationToken);
            _logger.LogInformation("IA #{Id} marked as trained.", request.IdIa);
        }
        else if (request.Action == "training_failed")
        {
            // trained_ia is left unchanged
            _logger.LogWarning("IA #{Id}: training failed — {Message}.", request.IdIa, request.Message);
        }
        else if (request.Action == "training_started")
        {
            ia.trained_ia = false;
            ia.date_training_ia = null;
            await _context.SaveChangesAsync(context.CancellationToken);
            _logger.LogInformation("IA #{Id}: training started.", request.IdIa);
        }
        else
        {
            _logger.LogInformation("IA #{Id}: received action '{Action}' with message: {Message}.",
                request.IdIa, request.Action, request.Message);
        }

        // Schedule a notification for terminal actions
        if ((request.Action == "training_completed" || request.Action == "training_failed")
            && request.RequestedBy > 0)
        {
            try
            {
                bool success = request.Action == "training_completed";
                var subject = success
                    ? $"IA #{request.IdIa} training completed successfully"
                    : $"IA #{request.IdIa} training failed";

                var body = success
                    ? $"Training for IA #{request.IdIa} completed successfully.\n" +
                      $"Accuracy: {request.Accuracy:P2} | Val. accuracy: {request.ValAccuracy:P2}\n" +
                      $"Loss: {request.Loss:F4} | Val. loss: {request.ValLoss:F4}\n" +
                      $"Epochs: {request.Epoch}"
                    : $"Training for IA #{request.IdIa} failed.\nDetails: {request.Message}";

                var notification = new
                {
                    Types = new[] { "email" },
                    RecipientUserId = request.RequestedBy,
                    Subject = subject,
                    Title = subject,
                    Body = body,
                };

                await _kafkaProducer.PublishAsync(
                    "notification-requests",
                    request.RequestedBy.ToString(),
                    JsonSerializer.Serialize(notification),
                    context.CancellationToken);

                _logger.LogInformation(
                    "Notification scheduled for user #{UserId} (IA #{Id}, action={Action}).",
                    request.RequestedBy, request.IdIa, request.Action);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish notification for user #{UserId} (IA #{Id}).",
                    request.RequestedBy, request.IdIa);
            }
        }

        return new UpdateIaStatusReply { Success = true };
    }
}
