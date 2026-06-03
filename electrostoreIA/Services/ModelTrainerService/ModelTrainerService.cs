using System.Collections.Concurrent;
using ElectrostoreIA.Enums;
using ElectrostoreIA.Dto;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Vision;
using ElectrostoreIA.Grpc;
using Grpc.Core;
using ElectrostoreIA.Services.ConfigCacheService;
using ElectrostoreIA.Services.FileService;

namespace ElectrostoreIA.Services.ModelTrainerService;

public class ModelTrainerService : IModelTrainerService
{
    private readonly MLContext _mlContext;
    private readonly string _modelDir;
    private readonly string _imageDir;
    private readonly int _defaultEpochs;
    private readonly int _defaultBatchSize;
    private readonly ILogger<ModelTrainerService> _logger;
    private readonly IaTrainingGrpc.IaTrainingGrpcClient _client;
    private readonly IFileService _fileService;

    public readonly ConcurrentDictionary<int, TrainingProgress> TrainingProgressMap = new();

    private readonly string[] SupportedExtensions;

    public ModelTrainerService(IConfiguration configuration, IaTrainingGrpc.IaTrainingGrpcClient client, ILogger<ModelTrainerService> logger, IConfigCacheService configCache, IFileService fileService)
    {
        _logger = logger;
        _client = client;
        _fileService = fileService;
        _mlContext = new MLContext(seed: 123);
        _modelDir = "models";
        _imageDir = "images";
        _defaultEpochs = configuration.GetValue<int>("DefaultEpochs", 10);
        _defaultBatchSize = configuration.GetValue<int>("DefaultBatchSize", 32);
        SupportedExtensions = configCache.AllowedImageExtensions.ToArray() ?? new[] { ".jpg", ".jpeg", ".png", ".bmp" };
    }

    public bool IsTrainingInProgress()
        => TrainingProgressMap.Values.Any(p => p.Status == TrainingStatus.InProgress);

    public TrainingProgress? GetTrainingStatus(int idModel)
        => TrainingProgressMap.TryGetValue(idModel, out var progress) ? progress : null;
    
    public bool SetTrainingProgressMap(int idModel, TrainingProgress progress)
    {
        TrainingProgressMap[idModel] = progress;
        return true;
    }

    public Task<bool> StartAndWaitAsync(int idModel, int requestedBy, CancellationToken ct = default)
    {
        TrainingProgressMap[idModel] = new TrainingProgress { Status = TrainingStatus.InProgress, RequestedBy = requestedBy };
        return Task.Run(() => TrainModel(idModel, requestedBy, ct), ct);
    }

    private async Task<bool> TrainModel(int idModel, int requestedBy, CancellationToken ct)
    {
        try
        {
            var dataDir = await DownloadTrainingImagesAsync(_imageDir, ct);
            var imageFiles = LoadImagePaths(dataDir);
            if (imageFiles.Count == 0)
            {
                throw new InvalidOperationException($"No training images found in {dataDir}");
            }
            var data = _mlContext.Data.LoadFromEnumerable(imageFiles);
            data = _mlContext.Data.ShuffleRows(data, seed: 123);
            var split = _mlContext.Data.TrainTestSplit(data, testFraction: 0.2);
            var workspacePath = Path.Combine(Path.GetTempPath(), $"mlnet_ia_{idModel}");
            Directory.CreateDirectory(workspacePath);
            float lastTrainAccuracy = 0, lastValAccuracy = 0, lastTrainLoss = 0, lastValLoss = 0, LearningRate = 0.01f;
            int epochCounter = 0, BatchProcessedCount = 0;
            var pipeline = _mlContext.Transforms.Conversion
                .MapValueToKey(outputColumnName: "LabelAsKey", inputColumnName: "Label")
                .Append(_mlContext.Transforms.LoadRawImageBytes(
                    outputColumnName: "Image",
                    imageFolder: null,
                    inputColumnName: "ImagePath"))
                .Append(_mlContext.MulticlassClassification.Trainers.ImageClassification(
                    new ImageClassificationTrainer.Options
                    {
                        FeatureColumnName = "Image",
                        LabelColumnName = "LabelAsKey",
                        ValidationSet = split.TestSet,
                        Arch = ImageClassificationTrainer.Architecture.ResnetV2101,
                        Epoch = _defaultEpochs,
                        BatchSize = _defaultBatchSize,
                        WorkspacePath = workspacePath,
                        MetricsCallback = (metrics) =>
                        {
                            if (metrics.Train == null)
                            {
                                return; // bottleneck
                            }
                            if (metrics.Train.DatasetUsed == ImageClassificationTrainer.ImageClassificationMetrics.Dataset.Train)
                            {
                                lastTrainAccuracy = metrics.Train.Accuracy;
                                lastTrainLoss = metrics.Train.CrossEntropy;
                                epochCounter = metrics.Train.Epoch;
                                LearningRate = metrics.Train.LearningRate;
                                BatchProcessedCount = metrics.Train.BatchProcessedCount;
                            }
                            else if (metrics.Train.DatasetUsed == ImageClassificationTrainer.ImageClassificationMetrics.Dataset.Validation)
                            {
                                lastValAccuracy = metrics.Train.Accuracy;
                                lastValLoss = metrics.Train.CrossEntropy;
                            }
                            TrainingProgressMap[idModel] = new TrainingProgress
                            {
                                Status = TrainingStatus.InProgress,
                                Message = "Training in progress",
                                Epoch = epochCounter,
                                Accuracy = lastTrainAccuracy,
                                ValAccuracy = lastValAccuracy,
                                Loss = lastTrainLoss,
                                ValLoss = lastValLoss,
                                LearningRate = LearningRate,
                                BatchProcessedCount = BatchProcessedCount,
                                RequestedBy = requestedBy
                            };
                            _logger.LogInformation("Model {Id} — Epoch {Epoch}: accuracy={Acc:F4} loss={Loss:F4} val_accuracy={ValAcc:F4} val_loss={ValLoss:F4} lr={LR:E2} batch_count={BatchCount}",
                                idModel, epochCounter, lastTrainAccuracy, lastTrainLoss, lastValAccuracy, lastValLoss, LearningRate, BatchProcessedCount);
                        }
                    }))
                .Append(_mlContext.Transforms.Conversion
                    .MapKeyToValue(outputColumnName: "PredictedLabel", inputColumnName: "LabelAsKey"));

            _logger.LogInformation("Starting training for model {Id} with {Count} images", idModel, imageFiles.Count);
            var model = pipeline.Fit(split.TrainSet);
            var modelPath = GetModelPath(idModel);
            using (var fs = new FileStream(modelPath, FileMode.Create))
            {
                _mlContext.Model.Save(model, split.TrainSet.Schema, fs);
            }
            var classNames = imageFiles.Select(f => f.Label).Distinct().OrderBy(x => x).ToList();
            var classNamesPath = GetClassNamesPath(idModel);
            await File.WriteAllLinesAsync(classNamesPath, classNames, ct);
            try
            {
                if (await UploadModelFileAsync(idModel, modelPath, ct))
                {
                    _logger.LogInformation("Model {Id} uploaded to FileService", idModel);
                }
                else
                {
                    _logger.LogWarning("Failed to upload model {Id} to FileService", idModel);
                }
                if (await UploadClassNamesAsync(idModel, classNames, ct))
                {
                    _logger.LogInformation("Class names for model {Id} uploaded to FileService", idModel);
                }
                else
                {
                    _logger.LogWarning("Failed to upload class names for model {Id} to FileService", idModel);
                }
            }
            catch (Exception uploadEx)
            {
                _logger.LogWarning(uploadEx, "Error uploading model {Id} or class names to FileService", idModel);
            }
            _logger.LogInformation("Training completed for model {Id}: accuracy={Acc:F4} val_accuracy={ValAcc:F4}", idModel, lastTrainAccuracy, lastValAccuracy);
            TrainingProgressMap[idModel] = new TrainingProgress
            {
                Status = TrainingStatus.Completed,
                Message = "Training completed",
                Epoch = epochCounter,
                Accuracy = lastTrainAccuracy,
                ValAccuracy = lastValAccuracy,
                Loss = lastTrainLoss,
                ValLoss = lastValLoss,
                LearningRate = LearningRate,
                BatchProcessedCount = BatchProcessedCount,
                RequestedBy = requestedBy
            };
            if (Directory.Exists(workspacePath))
            {
                Directory.Delete(workspacePath, true);
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during training of model {Id}", idModel);
            TrainingProgressMap[idModel] = new TrainingProgress
            {
                Status = TrainingStatus.Error,
                Message = ex.Message,
                RequestedBy = requestedBy
            };
            return false;
        }
    }

    private List<ImageData> LoadImagePaths(string dataDir)
    {
        var result = new List<ImageData>();
        foreach (var classDir in Directory.GetDirectories(dataDir))
        {
            var label = Path.GetFileName(classDir);
            foreach (var file in Directory.GetFiles(classDir))
            {
                if (SupportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                {
                    result.Add(new ImageData { Label = label, ImagePath = Path.GetFullPath(file) });
                }
            }
        }
        return result;
    }

    public string GetModelPath(int idModel) => Path.Combine(_modelDir, $"Model{idModel}.zip");

    public string GetClassNamesPath(int idModel) => Path.Combine(_modelDir, $"ItemList{idModel}.txt");

    public async Task<string> EnsureModelLocalAsync(int idModel)
    {
        var localPath = GetModelPath(idModel);
        if (!File.Exists(localPath))
        {
            var downloaded = await DownloadModelFileAsync(idModel, localPath);
            if (!downloaded)
                throw new FileNotFoundException($"Model {idModel} not found in FileService");
        }
        return localPath;
    }

    public async Task<string> EnsureClassNamesLocalAsync(int idModel)
    {
        var localPath = GetClassNamesPath(idModel);
        if (!File.Exists(localPath))
        {
            var names = await DownloadClassNamesAsync(idModel);
            if (names is null || names.Count == 0)
                throw new FileNotFoundException($"Class names for model {idModel} not found in FileService");
            await File.WriteAllLinesAsync(localPath, names);
        }
        return localPath;
    }

    public async Task DeleteModelFilesAsync(int idModel, CancellationToken ct = default)
    {
        await _fileService.DeleteFile(Path.Combine(_modelDir, $"Model{idModel}.zip"));
        await _fileService.DeleteFile(Path.Combine(_modelDir, $"ItemList{idModel}.txt"));
        var localModel = GetModelPath(idModel);
        if (File.Exists(localModel)) File.Delete(localModel);
        var localNames = GetClassNamesPath(idModel);
        if (File.Exists(localNames)) File.Delete(localNames);
        _logger.LogInformation("DeleteModelFiles: files deleted for model {Id}", idModel);
    }

    // ---- Training images ----

    private async Task<string> DownloadTrainingImagesAsync(string targetDir, CancellationToken ct = default)
    {
        int count = 0;
        var existingFiles = Directory.Exists(targetDir)
            ? Directory.EnumerateFiles(targetDir, "*", SearchOption.AllDirectories)
                       .Select(Path.GetFileName)
                       .Where(f => f is not null)
                       .Cast<string>()
                       .ToList()
            : new List<string>();
        var request = new StreamTrainingImagesRequest();
        request.ExistingFilenames.AddRange(existingFiles);
        using var call = _client.StreamTrainingImages(request, cancellationToken: ct);
        await foreach (var image in call.ResponseStream.ReadAllAsync(ct))
        {
            var classDir = Path.Combine(targetDir, image.Label);
            Directory.CreateDirectory(classDir);
            var localPath = Path.Combine(classDir, image.Filename);
            await File.WriteAllBytesAsync(localPath, image.Data.ToArray(), ct);
            count++;
        }
        _logger.LogInformation("DownloadTrainingImages: {Count} images téléchargées dans {Dir}", count, targetDir);
        return targetDir;
    }

    // ---- Model file management ----

    private async Task<bool> UploadModelFileAsync(int idIa, string localPath, CancellationToken ct = default)
    {
        try
        {
            using var fs = new FileStream(localPath, FileMode.Open, FileAccess.Read);
            await _fileService.SaveFile(_modelDir, $"Model{idIa}.zip", "application/zip", fs, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UploadModelFile failed for id {Id}", idIa);
            return false;
        }
    }

    private async Task<bool> DownloadModelFileAsync(int idIa, string localPath, CancellationToken ct = default)
    {
        try
        {
            var result = await _fileService.GetFile(Path.Combine(_modelDir, $"Model{idIa}.zip"));
            if (!result.Success || result.FileStream is null)
            {
                _logger.LogWarning("DownloadModelFile: model {Id} not found in FileService", idIa);
                return false;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
            using var fs = new FileStream(localPath, FileMode.Create, FileAccess.Write);
            await result.FileStream.CopyToAsync(fs, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DownloadModelFile failed for id {Id}", idIa);
            return false;
        }
    }

    private async Task<bool> UploadClassNamesAsync(int idIa, IEnumerable<string> classNames, CancellationToken ct = default)
    {
        try
        {
            var content = string.Join("\n", classNames);
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            await _fileService.SaveFile(_modelDir, $"ItemList{idIa}.txt", "text/plain", stream, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UploadClassNames failed for id {Id}", idIa);
            return false;
        }
    }

    private async Task<IReadOnlyList<string>?> DownloadClassNamesAsync(int idIa, CancellationToken ct = default)
    {
        try
        {
            var result = await _fileService.GetFile(Path.Combine(_modelDir, $"ItemList{idIa}.txt"));
            if (!result.Success || result.FileStream is null)
            {
                _logger.LogWarning("DownloadClassNames: class names for model {Id} not found in FileService", idIa);
                return null;
            }
            using var reader = new StreamReader(result.FileStream);
            var content = await reader.ReadToEndAsync(ct);
            return content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DownloadClassNames failed for id {Id}", idIa);
            return null;
        }
    }
}
