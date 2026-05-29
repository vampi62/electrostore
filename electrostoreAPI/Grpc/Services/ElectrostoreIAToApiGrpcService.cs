using ElectrostoreAPI.Services.ConfigService;
using ElectrostoreAPI.Services.FileService;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace ElectrostoreAPI.Grpc.Services;

public class ElectrostoreIAToApiGrpcService : IAToAPIGrpc.IAToAPIGrpcBase
{
    private readonly ApplicationDbContext _context;
    private readonly IFileService _fileService;
    private readonly ILogger<ElectrostoreIAToApiGrpcService> _logger;
    private readonly IConfigService _configService;

    public ElectrostoreIAToApiGrpcService(
        ApplicationDbContext context,
        IFileService fileService,
        ILogger<ElectrostoreIAToApiGrpcService> logger,
        IConfigService configService)
    {
        _context = context;
        _fileService = fileService;
        _logger = logger;
        _configService = configService;
    }

    // ---- Training images ----

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

    // ---- Config ----

    public override Task<IAGetConfigReply> GetConfig(IAGetConfigRequest request, ServerCallContext context)
    {
        var reply = new IAGetConfigReply { DemoMode = _configService.GetDemoMode() };
        reply.AllowedImageExtensions.AddRange(_configService.GetAllowedImageExtensions());
        return Task.FromResult(reply);
    }
}
