using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Kafka.Producer;
using Google.Protobuf;
using Grpc.Core;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace ElectrostoreAPI.Grpc.Services;

public class IaTrainingGrpcService : IaTrainingGrpc.IaTrainingGrpcBase
{
    private readonly ApplicationDbContext _context;
    private readonly IFileService _fileService;
    private readonly ILogger<IaTrainingGrpcService> _logger;
    private readonly IKafkaProducerService _kafkaProducer;

    public IaTrainingGrpcService(
        ApplicationDbContext context,
        IFileService fileService,
        ILogger<IaTrainingGrpcService> logger,
        IKafkaProducerService kafkaProducer)
    {
        _context = context;
        _fileService = fileService;
        _logger = logger;
        _kafkaProducer = kafkaProducer;
    }

    public override async Task StreamTrainingImages(
        StreamTrainingImagesRequest request,
        IServerStreamWriter<TrainingImage> responseStream,
        ServerCallContext context)
    {
        var existingSet = request.ExistingFilenames.Count > 0
            ? new HashSet<string>(request.ExistingFilenames, StringComparer.OrdinalIgnoreCase)
            : null;
        var images = await _context.Imgs
            .AsNoTracking()
            .ToListAsync(context.CancellationToken);
        if (existingSet is not null)
        {
            images = images.Where(img => !existingSet.Contains(Path.GetFileName(img.url_picture_img))).ToList();
        }
        _logger.LogInformation("StreamTrainingImages: streaming {Count} images (après filtrage)", images.Count);
        foreach (var img in images)
        {
            if (context.CancellationToken.IsCancellationRequested) break;
            try
            {
                var fileResult = await _fileService.GetFile(img.url_picture_img);
                if (!fileResult.Success || fileResult.FileStream is null)
                {
                    _logger.LogWarning("StreamTrainingImages: could not read image {Url}", img.url_picture_img);
                    continue;
                }
                using var ms = new MemoryStream();
                await fileResult.FileStream.CopyToAsync(ms, context.CancellationToken);
                await responseStream.WriteAsync(new TrainingImage
                {
                    Label = img.id_item.ToString(),
                    Filename = Path.GetFileName(img.url_picture_img),
                    Data = ByteString.CopyFrom(ms.ToArray())
                }, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StreamTrainingImages: error on image {Url}", img.url_picture_img);
            }
        }
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
