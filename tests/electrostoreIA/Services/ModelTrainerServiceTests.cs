using System.Text;
using ElectrostoreIA.Dto;
using ElectrostoreIA.Enums;
using ElectrostoreIA.Grpc;
using ElectrostoreIA.Services.ConfigCacheService;
using ElectrostoreIA.Services.FileService;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ModelTrainerServiceImpl = ElectrostoreIA.Services.ModelTrainerService.ModelTrainerService;

namespace ElectrostoreIA.Tests.Services;

public class ModelTrainerServiceTests
{
    private readonly Mock<IaTrainingGrpc.IaTrainingGrpcClient> _client = new();
    private readonly Mock<ILogger<ModelTrainerServiceImpl>> _logger = new();
    private readonly Mock<IConfigCacheService> _configCache = new();
    private readonly Mock<IFileService> _fileService = new();

    public ModelTrainerServiceTests()
    {
        _configCache.SetupGet(c => c.AllowedImageExtensions).Returns(new[] { ".jpg", ".jpeg", ".png", ".bmp" });
    }

    private ModelTrainerServiceImpl CreateService(IConfiguration? configuration = null)
    {
        configuration ??= new ConfigurationBuilder().Build();
        return new ModelTrainerServiceImpl(configuration, _client.Object, _logger.Object, _configCache.Object, _fileService.Object);
    }

    // Fake IAsyncStreamReader used to build an AsyncServerStreamingCall without a real gRPC channel.
    private sealed class FakeAsyncStreamReader<T> : IAsyncStreamReader<T>
    {
        private readonly IEnumerator<T> _enumerator;
        public FakeAsyncStreamReader(IEnumerable<T> items) => _enumerator = items.GetEnumerator();
        public T Current => _enumerator.Current;
        public Task<bool> MoveNext(CancellationToken cancellationToken) => Task.FromResult(_enumerator.MoveNext());
    }

    private static AsyncServerStreamingCall<TrainingImage> CreateStreamingCall(IEnumerable<TrainingImage> items)
    {
        return new AsyncServerStreamingCall<TrainingImage>(
            new FakeAsyncStreamReader<TrainingImage>(items),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    // ---- Path helpers ----

    [Fact]
    public void GetModelPath_ShouldCombineModelsDirectoryWithId()
    {
        var service = CreateService();
        Assert.Equal(Path.Combine("models", "Model42.zip"), service.GetModelPath(42));
    }

    [Fact]
    public void GetClassNamesPath_ShouldCombineModelsDirectoryWithId()
    {
        var service = CreateService();
        Assert.Equal(Path.Combine("models", "ItemList42.txt"), service.GetClassNamesPath(42));
    }

    // ---- Training progress map ----

    [Fact]
    public void IsTrainingInProgress_ShouldReturnZero_WhenNothingInProgress()
    {
        var service = CreateService();
        Assert.Equal(0, service.IsTrainingInProgress());
    }

    [Fact]
    public void IsTrainingInProgress_ShouldReturnModelId_WhenTrainingIsInProgress()
    {
        var service = CreateService();
        service.SetTrainingProgressMap(101, new TrainingProgress { Status = TrainingStatus.InProgress });

        Assert.Equal(101, service.IsTrainingInProgress());
    }

    [Fact]
    public void GetTrainingStatus_ShouldReturnNull_WhenNoProgressStored()
    {
        var service = CreateService();
        Assert.Null(service.GetTrainingStatus(999));
    }

    [Fact]
    public void SetTrainingProgressMap_ShouldStoreProgress_AndReturnTrue()
    {
        var service = CreateService();
        var progress = new TrainingProgress { Status = TrainingStatus.Completed, Message = "done" };

        var result = service.SetTrainingProgressMap(55, progress);

        Assert.True(result);
        Assert.Same(progress, service.GetTrainingStatus(55));
    }

    // ---- EnsureModelLocalAsync ----

    [Fact]
    public async Task EnsureModelLocalAsync_ShouldReturnLocalPath_WithoutDownloading_WhenFileAlreadyExists()
    {
        var service = CreateService();
        var localPath = service.GetModelPath(201);
        Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
        await File.WriteAllTextAsync(localPath, "existing model bytes");

        try
        {
            var result = await service.EnsureModelLocalAsync(201);

            Assert.Equal(localPath, result);
            _fileService.Verify(f => f.GetFile(It.IsAny<string>()), Times.Never);
        }
        finally
        {
            DeleteIfExists(localPath);
        }
    }

    [Fact]
    public async Task EnsureModelLocalAsync_ShouldDownloadAndSaveLocally_WhenMissing()
    {
        var service = CreateService();
        var localPath = service.GetModelPath(202);
        DeleteIfExists(localPath);
        var content = Encoding.UTF8.GetBytes("downloaded model bytes");
        _fileService
            .Setup(f => f.GetFile(Path.Combine("models", "Model202.zip")))
            .ReturnsAsync(new ElectrostoreIA.Dto.GetFileResult { Success = true, FileStream = new MemoryStream(content), MimeType = "application/zip" });

        try
        {
            var result = await service.EnsureModelLocalAsync(202);

            Assert.Equal(localPath, result);
            Assert.True(File.Exists(localPath));
            Assert.Equal(content, await File.ReadAllBytesAsync(localPath));
        }
        finally
        {
            DeleteIfExists(localPath);
        }
    }

    [Fact]
    public async Task EnsureModelLocalAsync_ShouldThrowFileNotFoundException_WhenDownloadFails()
    {
        var service = CreateService();
        var localPath = service.GetModelPath(203);
        DeleteIfExists(localPath);
        _fileService
            .Setup(f => f.GetFile(It.IsAny<string>()))
            .ReturnsAsync(new ElectrostoreIA.Dto.GetFileResult { Success = false, MimeType = "" });

        await Assert.ThrowsAsync<FileNotFoundException>(() => service.EnsureModelLocalAsync(203));
    }

    // ---- EnsureClassNamesLocalAsync ----

    [Fact]
    public async Task EnsureClassNamesLocalAsync_ShouldReturnLocalPath_WithoutDownloading_WhenFileAlreadyExists()
    {
        var service = CreateService();
        var localPath = service.GetClassNamesPath(301);
        Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
        await File.WriteAllTextAsync(localPath, "item1\nitem2");

        try
        {
            var result = await service.EnsureClassNamesLocalAsync(301);

            Assert.Equal(localPath, result);
            _fileService.Verify(f => f.GetFile(It.IsAny<string>()), Times.Never);
        }
        finally
        {
            DeleteIfExists(localPath);
        }
    }

    [Fact]
    public async Task EnsureClassNamesLocalAsync_ShouldDownloadAndWriteLines_WhenMissing()
    {
        var service = CreateService();
        var localPath = service.GetClassNamesPath(302);
        DeleteIfExists(localPath);
        var content = Encoding.UTF8.GetBytes("item1\nitem2\nitem3");
        _fileService
            .Setup(f => f.GetFile(Path.Combine("models", "ItemList302.txt")))
            .ReturnsAsync(new ElectrostoreIA.Dto.GetFileResult { Success = true, FileStream = new MemoryStream(content), MimeType = "text/plain" });

        try
        {
            var result = await service.EnsureClassNamesLocalAsync(302);

            Assert.Equal(localPath, result);
            var lines = await File.ReadAllLinesAsync(localPath);
            Assert.Equal(new[] { "item1", "item2", "item3" }, lines);
        }
        finally
        {
            DeleteIfExists(localPath);
        }
    }

    [Fact]
    public async Task EnsureClassNamesLocalAsync_ShouldThrowFileNotFoundException_WhenDownloadReturnsNoData()
    {
        var service = CreateService();
        var localPath = service.GetClassNamesPath(303);
        DeleteIfExists(localPath);
        _fileService
            .Setup(f => f.GetFile(It.IsAny<string>()))
            .ReturnsAsync(new ElectrostoreIA.Dto.GetFileResult { Success = false, MimeType = "" });

        await Assert.ThrowsAsync<FileNotFoundException>(() => service.EnsureClassNamesLocalAsync(303));
    }

    // ---- DeleteModelFilesAsync ----

    [Fact]
    public async Task DeleteModelFilesAsync_ShouldDeleteRemoteAndLocalFiles()
    {
        var service = CreateService();
        var modelPath = service.GetModelPath(401);
        var classNamesPath = service.GetClassNamesPath(401);
        Directory.CreateDirectory(Path.GetDirectoryName(modelPath)!);
        await File.WriteAllTextAsync(modelPath, "model");
        await File.WriteAllTextAsync(classNamesPath, "names");
        _fileService.Setup(f => f.DeleteFile(It.IsAny<string>())).Returns(Task.CompletedTask);

        await service.DeleteModelFilesAsync(401);

        _fileService.Verify(f => f.DeleteFile(Path.Combine("models", "Model401.zip")), Times.Once);
        _fileService.Verify(f => f.DeleteFile(Path.Combine("models", "ItemList401.txt")), Times.Once);
        Assert.False(File.Exists(modelPath));
        Assert.False(File.Exists(classNamesPath));
    }

    // ---- StartAndWaitAsync (early-failure path, no real ML training) ----

    [Fact]
    public async Task StartAndWaitAsync_ShouldSetErrorStatus_WhenNoTrainingImagesAreStreamed()
    {
        var service = CreateService();
        _client
            .Setup(c => c.StreamTrainingImages(It.IsAny<StreamTrainingImagesRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateStreamingCall(Enumerable.Empty<TrainingImage>()));

        // TrainModel writes downloaded images to a hardcoded "images" directory relative to the
        // working directory; with an empty stream that directory is never created, so it must
        // exist (empty) upfront for LoadImagePaths to hit the intended "no images" branch instead
        // of a DirectoryNotFoundException from Directory.GetDirectories.
        var imageDir = Path.Combine(Directory.GetCurrentDirectory(), "images");
        if (Directory.Exists(imageDir)) Directory.Delete(imageDir, true);
        Directory.CreateDirectory(imageDir);

        try
        {
            var result = await service.StartAndWaitAsync(999001, requestedBy: 7);

            Assert.False(result);
            var status = service.GetTrainingStatus(999001);
            Assert.NotNull(status);
            Assert.Equal(TrainingStatus.Error, status!.Status);
            Assert.Contains("No training images found", status.Message);
            Assert.Equal(7, status.RequestedBy);
        }
        finally
        {
            Directory.Delete(imageDir, true);
        }
    }

}
