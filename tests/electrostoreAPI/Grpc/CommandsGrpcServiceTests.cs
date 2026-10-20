using ElectrostoreAPI.Grpc;
using ElectrostoreAPI.Grpc.Services;
using ElectrostoreAPI.Services.CommandService;
using ElectrostoreAPI.Tests.Utils;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElectrostoreAPI.Tests.Grpc;

public class CommandsGrpcServiceTests
{
    private readonly Mock<ICommandService> _commandService = new();
    private readonly Mock<ILogger<CommandsGrpcService>> _logger = new();

    private CommandsGrpcService CreateService() => new(_commandService.Object, _logger.Object);

    [Fact]
    public async Task UpdateCommandStatus_ShouldForwardToServiceAndReturnSuccess()
    {
        var service = CreateService();

        var reply = await service.UpdateCommandStatus(
            new UpdateCommandStatusRequest { TrackingNumber = "TRK123", KeyCarrier = 2, Action = "delivered" },
            TestServerCallContext.Create());

        Assert.True(reply.Success);
        _commandService.Verify(s => s.UpdateCommandStatusByTracking("TRK123", 2, "delivered"), Times.Once);
    }

    [Theory]
    [InlineData("", 2)]
    [InlineData("   ", 2)]
    [InlineData("TRK123", 0)]
    public async Task UpdateCommandStatus_ShouldReturnFailure_WhenRequestIsInvalid(string tracking, int carrier)
    {
        var service = CreateService();

        var reply = await service.UpdateCommandStatus(
            new UpdateCommandStatusRequest { TrackingNumber = tracking, KeyCarrier = carrier, Action = "delivered" },
            TestServerCallContext.Create());

        Assert.False(reply.Success);
        _commandService.Verify(s => s.UpdateCommandStatusByTracking(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCommandStatus_ShouldReturnFailure_WhenServiceThrows()
    {
        _commandService
            .Setup(s => s.UpdateCommandStatusByTracking(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .ThrowsAsync(new KeyNotFoundException());
        var service = CreateService();

        var reply = await service.UpdateCommandStatus(
            new UpdateCommandStatusRequest { TrackingNumber = "TRK123", KeyCarrier = 2, Action = "delivered" },
            TestServerCallContext.Create());

        Assert.False(reply.Success);
    }
}
