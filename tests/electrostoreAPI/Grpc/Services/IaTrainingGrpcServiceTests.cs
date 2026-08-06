using Grpc.Core;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Grpc;
using ElectrostoreAPI.Grpc.Services;
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Services.IAService;
using ElectrostoreAPI.Services.ImgService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Grpc.Services;

public class IaTrainingGrpcServiceTests
{
    private readonly Mock<IFileService> _fileService;
    private readonly Mock<IIAService> _iaService;
    private readonly Mock<IImgService> _imgService;
    private readonly IaTrainingGrpcService _service;

    public IaTrainingGrpcServiceTests()
    {
        _fileService = new Mock<IFileService>();
        _iaService = new Mock<IIAService>();
        _imgService = new Mock<IImgService>();
        _service = new IaTrainingGrpcService(_fileService.Object, _iaService.Object, _imgService.Object, new LoggerFactory().CreateLogger<IaTrainingGrpcService>());
    }

    [Fact]
    public async Task StreamTrainingImages_ShouldPassNullSet_WhenNoExistingFilenames()
    {
        // Arrange
        var request = new StreamTrainingImagesRequest();
        var responseStream = new Mock<IServerStreamWriter<TrainingImage>>();

        // Act
        await _service.StreamTrainingImages(request, responseStream.Object, TestServerCallContext.Create());

        // Assert
        _imgService.Verify(s => s.StreamTrainingImagesAsync(responseStream.Object, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StreamTrainingImages_ShouldPassCaseInsensitiveSet_WhenExistingFilenamesProvided()
    {
        // Arrange
        var request = new StreamTrainingImagesRequest();
        request.ExistingFilenames.Add("Foo.jpg");
        request.ExistingFilenames.Add("bar.jpg");
        var responseStream = new Mock<IServerStreamWriter<TrainingImage>>();
        HashSet<string>? capturedSet = null;
        _imgService.Setup(s => s.StreamTrainingImagesAsync(responseStream.Object, It.IsAny<HashSet<string>?>(), It.IsAny<CancellationToken>()))
            .Callback<IAsyncStreamWriter<TrainingImage>, HashSet<string>?, CancellationToken>((_, set, _) => capturedSet = set)
            .Returns(Task.CompletedTask);

        // Act
        await _service.StreamTrainingImages(request, responseStream.Object, TestServerCallContext.Create());

        // Assert
        Assert.NotNull(capturedSet);
        Assert.Contains("foo.jpg", capturedSet!, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("FOO.JPG", capturedSet!, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateIaStatus_ShouldMapRequestToStatusDto_AndReturnServiceResult()
    {
        // Arrange
        var request = new UpdateIaStatusRequest
        {
            IdIa = 3,
            Action = "training_completed",
            Message = "done",
            Epoch = 10,
            Accuracy = 0.9f,
            ValAccuracy = 0.85f,
            Loss = 0.1f,
            ValLoss = 0.15f,
            RequestedBy = 42
        };
        IAStatusDto? captured = null;
        _iaService.Setup(s => s.UpdateIaStatusAsync(3, It.IsAny<IAStatusDto>(), 42, It.IsAny<CancellationToken>()))
            .Callback<int, IAStatusDto, int?, CancellationToken>((_, dto, _, _) => captured = dto)
            .ReturnsAsync(true);

        // Act
        var reply = await _service.UpdateIaStatus(request, TestServerCallContext.Create());

        // Assert
        Assert.True(reply.Success);
        Assert.NotNull(captured);
        Assert.Equal("training_completed", captured!.Status);
        Assert.Equal("done", captured.Message);
        Assert.Equal(10, captured.Epoch);
        Assert.Equal(0.9f, captured.Accuracy);
        Assert.Equal(0.85f, captured.ValAccuracy);
        Assert.Equal(0.1f, captured.Loss);
        Assert.Equal(0.15f, captured.ValLoss);
    }

    [Fact]
    public async Task UpdateIaStatus_ShouldReturnFalse_WhenServiceReturnsFalse()
    {
        // Arrange
        var request = new UpdateIaStatusRequest { IdIa = 999, Action = "training_failed", Message = "not found" };
        _iaService.Setup(s => s.UpdateIaStatusAsync(999, It.IsAny<IAStatusDto>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var reply = await _service.UpdateIaStatus(request, TestServerCallContext.Create());

        // Assert
        Assert.False(reply.Success);
    }
}
