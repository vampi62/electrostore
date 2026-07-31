using ElectrostoreWORKER.Grpc;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ConfigCacheServiceImpl = ElectrostoreWORKER.Services.ConfigCacheService.ConfigCacheService;

namespace ElectrostoreWORKER.Tests.Services;

public class ConfigCacheServiceTests
{
    private readonly Mock<ConfigGrpc.ConfigGrpcClient> _configGrpcClient;
    private readonly Mock<ILogger<ConfigCacheServiceImpl>> _logger;

    public ConfigCacheServiceTests()
    {
        _configGrpcClient = new Mock<ConfigGrpc.ConfigGrpcClient>();
        _logger = new Mock<ILogger<ConfigCacheServiceImpl>>();
    }

    private static AsyncUnaryCall<TResponse> CreateAsyncUnaryCall<TResponse>(TResponse response)
    {
        return new AsyncUnaryCall<TResponse>(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
    }

    private static AsyncUnaryCall<TResponse> CreateFailingAsyncUnaryCall<TResponse>(Exception exception)
    {
        return new AsyncUnaryCall<TResponse>(
            Task.FromException<TResponse>(exception),
            Task.FromException<Metadata>(exception),
            () => Status.DefaultCancelled,
            () => new Metadata(),
            () => { });
    }

    [Fact]
    public async Task StartAsync_ShouldSetDemoModeFromApiReply()
    {
        // Arrange
        _configGrpcClient
            .Setup(c => c.GetConfigAsync(It.IsAny<GetConfigRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(new GetConfigReply { DemoMode = true }));
        var service = new ConfigCacheServiceImpl(_configGrpcClient.Object, _logger.Object);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        Assert.True(service.DemoMode);
    }

    [Fact]
    public async Task StartAsync_ShouldSetDemoModeFalse_WhenApiReturnsFalse()
    {
        // Arrange
        _configGrpcClient
            .Setup(c => c.GetConfigAsync(It.IsAny<GetConfigRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(new GetConfigReply { DemoMode = false }));
        var service = new ConfigCacheServiceImpl(_configGrpcClient.Object, _logger.Object);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        Assert.False(service.DemoMode);
    }

    [Fact]
    public async Task StartAsync_ShouldFallBackToDemoModeFalse_WhenApiCallFails()
    {
        // Unlike electrostoreNOTIF (which defaults to demo mode when the API is unreachable),
        // electrostoreWORKER falls back to DemoMode=false ("default values applied").
        // Arrange
        _configGrpcClient
            .Setup(c => c.GetConfigAsync(It.IsAny<GetConfigRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateFailingAsyncUnaryCall<GetConfigReply>(new RpcException(new Status(StatusCode.Unavailable, "down"))));
        var service = new ConfigCacheServiceImpl(_configGrpcClient.Object, _logger.Object);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        Assert.False(service.DemoMode);
    }

    [Fact]
    public async Task StopAsync_ShouldCompleteImmediately()
    {
        // Arrange
        var service = new ConfigCacheServiceImpl(_configGrpcClient.Object, _logger.Object);

        // Act
        var task = service.StopAsync(CancellationToken.None);

        // Assert
        Assert.True(task.IsCompletedSuccessfully);
        await task;
    }
}
