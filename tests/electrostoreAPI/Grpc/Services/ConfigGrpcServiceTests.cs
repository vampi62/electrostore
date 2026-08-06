using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ElectrostoreAPI.Grpc;
using ElectrostoreAPI.Grpc.Services;
using ElectrostoreAPI.Services.ConfigService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Grpc.Services;

public class ConfigGrpcServiceTests
{
    private readonly Mock<IConfigService> _configService;
    private readonly ConfigGrpcService _service;

    public ConfigGrpcServiceTests()
    {
        _configService = new Mock<IConfigService>();
        _service = new ConfigGrpcService(new LoggerFactory().CreateLogger<ConfigGrpcService>(), _configService.Object);
    }

    [Fact]
    public async Task GetConfig_ShouldReturnDemoModeAndExtensions_FromConfigService()
    {
        // Arrange
        _configService.Setup(s => s.GetDemoMode()).Returns(true);
        _configService.Setup(s => s.GetAllowedImageExtensions()).Returns(new[] { ".png", ".jpg" });

        // Act
        var reply = await _service.GetConfig(new GetConfigRequest(), TestServerCallContext.Create());

        // Assert
        Assert.True(reply.DemoMode);
        Assert.Equal(new[] { ".png", ".jpg" }, reply.AllowedImageExtensions);
    }

    [Fact]
    public async Task GetConfig_ShouldReturnFalseAndEmptyExtensions_WhenConfigServiceReturnsDefaults()
    {
        // Arrange
        _configService.Setup(s => s.GetDemoMode()).Returns(false);
        _configService.Setup(s => s.GetAllowedImageExtensions()).Returns(Array.Empty<string>());

        // Act
        var reply = await _service.GetConfig(new GetConfigRequest(), TestServerCallContext.Create());

        // Assert
        Assert.False(reply.DemoMode);
        Assert.Empty(reply.AllowedImageExtensions);
    }
}
