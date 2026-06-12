using ElectrostoreIA.Enums;
using Grpc.Core;
using ElectrostoreIA.Services.ModelTrainerService;
using ElectrostoreIA.Services.ImageDetectorService;
using ElectrostoreIA.Services.ConfigCacheService;

namespace ElectrostoreIA.Grpc.Services;

public class IaCmdService : IaCmdGrpc.IaCmdGrpcBase
{
    private readonly IModelTrainerService _trainerService;
    private readonly IImageDetectorService _detectorService;
    private readonly ILogger<IaCmdService> _logger;

    public IaCmdService(
        IModelTrainerService trainerService,
        IImageDetectorService detectorService,
        IConfigCacheService configCache,
        ILogger<IaCmdService> logger)
    {
        _trainerService = trainerService;
        _detectorService = detectorService;
        _logger = logger;
    }

    public override Task<StatusReply> GetStatus(StatusRequest request, ServerCallContext context)
    {
        var progress = _trainerService.GetTrainingStatus(request.IdModel);
        if (progress is null)
        {
            return Task.FromResult(new StatusReply
            {
                Status = "not planned",
                Message = "No training planned for this model"
            });
        }
        return Task.FromResult(new StatusReply
        {
            Status = progress.Status switch
            {
                TrainingStatus.NotPlanned => "not planned",
                TrainingStatus.InWaiting  => "in waiting",
                TrainingStatus.InProgress => "in progress",
                TrainingStatus.Completed  => "completed",
                TrainingStatus.Error      => "error",
                _                         => "unknown"
            },
            Message     = progress.Message,
            Epoch       = progress.Epoch,
            Accuracy    = progress.Accuracy,
            ValAccuracy = progress.ValAccuracy,
            Loss        = progress.Loss,
            ValLoss     = progress.ValLoss
        });
    }

    public override async Task<DetectReply> Detect(DetectRequest request, ServerCallContext context)
    {
        try
        {
            using var stream = new MemoryStream(request.ImageData.ToByteArray());
            var (predictedClass, confidence) = await _detectorService.DetectAsync(
                request.IdModel, stream, context.CancellationToken);
            return new DetectReply
            {
                PredictedClass = predictedClass,
                Confidence = confidence
            };
        }
        catch (FileNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting with model {Id}", request.IdModel);
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }
}
