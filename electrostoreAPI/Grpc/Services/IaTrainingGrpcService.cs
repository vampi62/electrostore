using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Services.IAService;
using ElectrostoreAPI.Services.ImgService;
using Grpc.Core;

namespace ElectrostoreAPI.Grpc.Services;

public class IaTrainingGrpcService : IaTrainingGrpc.IaTrainingGrpcBase
{
    private readonly IFileService _fileService;
    private readonly IIAService _iaService;
    private readonly IImgService _imgService;
    private readonly ILogger<IaTrainingGrpcService> _logger;

    public IaTrainingGrpcService(
        IFileService fileService,
        IIAService iaService,
        IImgService imgService,
        ILogger<IaTrainingGrpcService> logger)
    {
        _fileService = fileService;
        _iaService = iaService;
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
        await _imgService.StreamTrainingImagesAsync(responseStream, existingSet, context.CancellationToken);
    }

    public override async Task<UpdateIaStatusReply> UpdateIaStatus(
        UpdateIaStatusRequest request, ServerCallContext context)
    {
        var iaStatus = new IAStatusDto
        {
            Status = request.Action,
            Message = request.Message,
            Epoch = request.Epoch,
            Accuracy = request.Accuracy,
            ValAccuracy = request.ValAccuracy,
            Loss = request.Loss,
            ValLoss = request.ValLoss
        };
        var result = await _iaService.UpdateIaStatusAsync(request.IdIa, iaStatus, request.RequestedBy, context.CancellationToken);
        return new UpdateIaStatusReply { Success = result };
    }
}
