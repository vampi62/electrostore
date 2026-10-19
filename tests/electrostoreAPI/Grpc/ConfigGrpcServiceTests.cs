using ElectrostoreAPI.Grpc;
using ElectrostoreAPI.Grpc.Services;
using ElectrostoreAPI.Services.ConfigService;
using ElectrostoreAPI.Tests.Utils;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElectrostoreAPI.Tests.Grpc;

public class ConfigGrpcServiceTests
{
    private readonly Mock<IConfigService> _configService = new();
    private readonly Mock<ILogger<ConfigGrpcService>> _logger = new();

    private ConfigGrpcService CreateService() => new(_configService.Object, _logger.Object);

    [Fact]
    public async Task GetConfig_ShouldReturnDemoModeAndImageExtensions()
    {
        _configService.Setup(s => s.GetDemoMode()).Returns(true);
        _configService.Setup(s => s.GetAllowedImageExtensions()).Returns([".png", ".jpg"]);
        var service = CreateService();

        var reply = await service.GetConfig(new GetConfigRequest(), TestServerCallContext.Create());

        Assert.True(reply.DemoMode);
        Assert.Equal([".png", ".jpg"], reply.AllowedImageExtensions);
    }

    [Fact]
    public async Task GetConfig_ShouldReturnDefaults_WhenDemoModeOffAndNoExtensions()
    {
        _configService.Setup(s => s.GetDemoMode()).Returns(false);
        _configService.Setup(s => s.GetAllowedImageExtensions()).Returns([]);
        var service = CreateService();

        var reply = await service.GetConfig(new GetConfigRequest(), TestServerCallContext.Create());

        Assert.False(reply.DemoMode);
        Assert.Empty(reply.AllowedImageExtensions);
    }
}
