using ElectrostoreIA.Services.ImageDetectorService;
using ElectrostoreIA.Services.ModelTrainerService;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElectrostoreIA.Tests.Services;

public class ImageDetectorServiceTests
{
    private readonly Mock<IModelTrainerService> _trainerService = new();
    private readonly Mock<ILogger<ImageDetectorService>> _logger = new();

    private ImageDetectorService CreateService() => new(_trainerService.Object, _logger.Object);

    [Fact]
    public async Task DetectAsync_ShouldReturnFallback_WhenModelFileDoesNotExist()
    {
        // Arrange
        var service = CreateService();
        _trainerService.Setup(t => t.EnsureModelLocalAsync(1)).ReturnsAsync("models/Model1.zip");
        var missingPath = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.zip");
        _trainerService.Setup(t => t.GetModelPath(1)).Returns(missingPath);

        // Act
        var (predictedClass, confidence) = await service.DetectAsync(1, new MemoryStream(new byte[] { 1, 2, 3 }));

        // Assert
        Assert.Equal(-1, predictedClass);
        Assert.Equal(0f, confidence);
        _trainerService.Verify(t => t.EnsureModelLocalAsync(1), Times.Once);
    }

    [Fact]
    public async Task DetectAsync_ShouldReturnFallback_WhenModelFileIsNotAValidMlModel()
    {
        // Arrange
        var service = CreateService();
        _trainerService.Setup(t => t.EnsureModelLocalAsync(2)).ReturnsAsync("models/Model2.zip");
        var invalidModelPath = Path.Combine(Path.GetTempPath(), $"invalid_{Guid.NewGuid():N}.zip");
        await File.WriteAllBytesAsync(invalidModelPath, new byte[] { 0x00, 0x01, 0x02, 0x03 });
        _trainerService.Setup(t => t.GetModelPath(2)).Returns(invalidModelPath);

        try
        {
            // Act
            var (predictedClass, confidence) = await service.DetectAsync(2, new MemoryStream(new byte[] { 1, 2, 3 }));

            // Assert
            Assert.Equal(-1, predictedClass);
            Assert.Equal(0f, confidence);
        }
        finally
        {
            File.Delete(invalidModelPath);
        }
    }

    [Fact]
    public async Task DetectAsync_ShouldPropagateException_WhenEnsureModelLocalAsyncThrows()
    {
        // EnsureModelLocalAsync is called before the try/catch block in DetectAsync, so unlike
        // failures further down (missing/invalid model file), this exception is not swallowed
        // into the (-1, 0) fallback - it propagates to the caller.
        // Arrange
        var service = CreateService();
        _trainerService.Setup(t => t.EnsureModelLocalAsync(3)).ThrowsAsync(new FileNotFoundException("model missing"));

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => service.DetectAsync(3, new MemoryStream(new byte[] { 1, 2, 3 })));
        _trainerService.Verify(t => t.GetModelPath(It.IsAny<int>()), Times.Never);
    }
}
