using System.Collections.Concurrent;
using Microsoft.ML;
using ElectrostoreIA.Services.ModelTrainerService;
using ElectrostoreIA.Dto;

namespace ElectrostoreIA.Services.ImageDetectorService;

public class ImageDetectorService : IImageDetectorService
{
    private readonly MLContext _mlContext;
    private readonly IModelTrainerService _trainerService;
    private readonly ILogger<ImageDetectorService> _logger;
    private readonly ConcurrentDictionary<int, ITransformer> _modelCache = new();

    public ImageDetectorService(IModelTrainerService trainerService, ILogger<ImageDetectorService> logger)
    {
        _mlContext = new MLContext(seed: 0);
        _trainerService = trainerService;
        _logger = logger;
    }

    public async Task<(int PredictedClass, float Confidence)> DetectAsync(
        int idModel,
        Stream imageStream,
        CancellationToken ct = default)
    {
        await _trainerService.EnsureModelLocalAsync(idModel);
        var tempImagePath = Path.Combine(Path.GetTempPath(), $"ia_detect_{Guid.NewGuid()}.jpg");
        try
        {
            await using (var fs = new FileStream(tempImagePath, FileMode.Create))
            {
                await imageStream.CopyToAsync(fs, ct);
            }
            if (!_modelCache.TryGetValue(idModel, out var model))
            {
                var modelPath = _trainerService.GetModelPath(idModel);
                using var modelFs = new FileStream(modelPath, FileMode.Open, FileAccess.Read);
                model = _mlContext.Model.Load(modelFs, out _);
                _modelCache[idModel] = model;
            }
            var engine = _mlContext.Model.CreatePredictionEngine<ImageData, ImagePrediction>(model);
            var result = engine.Predict(new ImageData
            {
                ImagePath = tempImagePath,
                Label = string.Empty
            });
            if (result.PredictedLabel is null || result.Score is null)
            {
                throw new InvalidOperationException("Model prediction returned null label or score");
            }
            if (!int.TryParse(result.PredictedLabel, out var predictedClass))
            {
                throw new InvalidOperationException(
                    $"Predicted label '{result.PredictedLabel}' is not a valid integer item id");
            }
            var confidence = result.Score.Max() * 100f;
            _logger.LogInformation("Model {Id} → class {Class} ({Conf:F2}%)", idModel, predictedClass, confidence);
            return (predictedClass, confidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during detection with model {Id}", idModel);
            return (-1, 0); // Return -1 for class and 0% confidence on error
        }
        finally
        {
            if (File.Exists(tempImagePath))
            {
                File.Delete(tempImagePath);
            }
        }
    }
}