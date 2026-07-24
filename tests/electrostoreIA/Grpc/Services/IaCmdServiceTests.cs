using ElectrostoreIA.Dto;
using ElectrostoreIA.Enums;
using ElectrostoreIA.Grpc;
using ElectrostoreIA.Grpc.Services;
using ElectrostoreIA.Services.ConfigCacheService;
using ElectrostoreIA.Services.ImageDetectorService;
using ElectrostoreIA.Services.ModelTrainerService;
using ElectrostoreIA.Tests.Utils;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElectrostoreIA.Tests.Grpc.Services;

public class IaCmdServiceTests
{
    private readonly Mock<IModelTrainerService> _trainerService = new();
    private readonly Mock<IImageDetectorService> _detectorService = new();
    private readonly Mock<IConfigCacheService> _configCache = new();
    private readonly Mock<ILogger<IaCmdService>> _logger = new();

    private IaCmdService CreateService() =>
        new(_trainerService.Object, _detectorService.Object, _configCache.Object, _logger.Object);

    // ---- GetStatus ----

    [Fact]
    public async Task GetStatus_ShouldReturnNotPlanned_WhenNoProgressStored()
    {
        // Arrange
        var service = CreateService();
        _trainerService.Setup(t => t.GetTrainingStatus(1)).Returns((TrainingProgress?)null);

        // Act
        var reply = await service.GetStatus(new StatusRequest { IdModel = 1 }, TestServerCallContext.Create());

        // Assert
        Assert.Equal("not planned", reply.Status);
        Assert.Equal("No training planned for this model", reply.Message);
    }

    [Theory]
    [InlineData(TrainingStatus.NotPlanned, "not planned")]
    [InlineData(TrainingStatus.InWaiting, "in waiting")]
    [InlineData(TrainingStatus.InProgress, "in progress")]
    [InlineData(TrainingStatus.Completed, "completed")]
    [InlineData(TrainingStatus.Error, "error")]
    public async Task GetStatus_ShouldMapStatusEnum_ToExpectedString(TrainingStatus status, string expected)
    {
        // Arrange
        var service = CreateService();
        _trainerService.Setup(t => t.GetTrainingStatus(2)).Returns(new TrainingProgress
        {
            Status = status,
            Message = "some message",
            Epoch = 5,
            Accuracy = 0.9f,
            ValAccuracy = 0.8f,
            Loss = 0.1f,
            ValLoss = 0.2f
        });

        // Act
        var reply = await service.GetStatus(new StatusRequest { IdModel = 2 }, TestServerCallContext.Create());

        // Assert
        Assert.Equal(expected, reply.Status);
        Assert.Equal("some message", reply.Message);
        Assert.Equal(5, reply.Epoch);
        Assert.Equal(0.9f, reply.Accuracy);
        Assert.Equal(0.8f, reply.ValAccuracy);
        Assert.Equal(0.1f, reply.Loss);
        Assert.Equal(0.2f, reply.ValLoss);
    }

    // ---- Detect ----

    [Fact]
    public async Task Detect_ShouldReturnPrediction_OnSuccess()
    {
        // Arrange
        var service = CreateService();
        _detectorService
            .Setup(d => d.DetectAsync(3, It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((5, 87.5f));
        var request = new DetectRequest { IdModel = 3, ImageData = ByteString.CopyFrom(new byte[] { 1, 2, 3 }) };

        // Act
        var reply = await service.Detect(request, TestServerCallContext.Create());

        // Assert
        Assert.Equal(5, reply.PredictedClass);
        Assert.Equal(87.5f, reply.Confidence);
    }

    [Fact]
    public async Task Detect_ShouldThrowNotFoundRpcException_WhenModelFileIsMissing()
    {
        // Arrange
        var service = CreateService();
        _detectorService
            .Setup(d => d.DetectAsync(4, It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("model missing"));
        var request = new DetectRequest { IdModel = 4, ImageData = ByteString.CopyFrom(new byte[] { 1, 2, 3 }) };

        // Act
        var exception = await Assert.ThrowsAsync<RpcException>(() => service.Detect(request, TestServerCallContext.Create()));

        // Assert
        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task Detect_ShouldThrowInternalRpcException_OnUnexpectedError()
    {
        // Arrange
        var service = CreateService();
        _detectorService
            .Setup(d => d.DetectAsync(5, It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var request = new DetectRequest { IdModel = 5, ImageData = ByteString.CopyFrom(new byte[] { 1, 2, 3 }) };

        // Act
        var exception = await Assert.ThrowsAsync<RpcException>(() => service.Detect(request, TestServerCallContext.Create()));

        // Assert
        Assert.Equal(StatusCode.Internal, exception.StatusCode);
    }
}
