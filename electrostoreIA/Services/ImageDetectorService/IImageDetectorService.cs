

namespace ElectrostoreIA.Services.ImageDetectorService;

public interface IImageDetectorService
{
    public Task<(int PredictedClass, float Confidence)> DetectAsync(
        int idModel,
        Stream imageStream,
        CancellationToken ct = default);
}