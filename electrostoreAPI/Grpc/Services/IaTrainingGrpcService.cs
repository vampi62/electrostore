using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Services.AIService;
using ElectrostoreAPI.Services.ImgService;
using Grpc.Core;

namespace ElectrostoreAPI.Grpc.Services;

public class IaTrainingGrpcService : IaTrainingGrpc.IaTrainingGrpcBase
{
    private readonly IAIService _aiService;
    private readonly IImgService _imgService;
    private readonly ILogger<IaTrainingGrpcService> _logger;

    public IaTrainingGrpcService(
        IAIService aiService,
        IImgService imgService,
        ILogger<IaTrainingGrpcService> logger)
    {
        _aiService = aiService;
        _imgService = imgService;
        _logger = logger;
    }

    public override async Task StreamTrainingImages(
        StreamTrainingImagesRequest request,
        IServerStreamWriter<TrainingImage> responseStream,
        ServerCallContext context)
    {
        var existingSet = request.ExistingFilenames.Count > 0
            ? new HashSet<string>(request.ExistingFilenames, StringComparer.OrdinalIgnoreCase)
            : null;
        _logger.LogInformation("StreamTrainingImages: starting stream, {Count} filename(s) already known", request.ExistingFilenames.Count);
        await _imgService.StreamTrainingImagesAsync(responseStream, existingSet, context.CancellationToken);
    }

    public override async Task<UpdateIaStatusReply> UpdateIaStatus(
        UpdateIaStatusRequest request, ServerCallContext context)
    {
        var aiStatus = new AIStatusDto
        {
            Status = request.Action,
            Message = request.Message,
            Epoch = request.Epoch,
            Accuracy = request.Accuracy,
            ValAccuracy = request.ValAccuracy,
            Loss = request.Loss,
            ValLoss = request.ValLoss
        };
        var result = await _aiService.UpdateIaStatusAsync(request.IdIa, aiStatus, request.RequestedBy, context.CancellationToken);
        if (!result)
        {
            _logger.LogWarning("UpdateIaStatus: update failed for AI {Id} (status={Status})", request.IdIa, request.Action);
        }
        return new UpdateIaStatusReply { Success = result };
    }
}
