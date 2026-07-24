using Grpc.Core;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ElectrostoreAPI.Grpc;
using ElectrostoreAPI.Grpc.Services;
using ElectrostoreAPI.Services.CommandService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Grpc.Services;

public class CommandsGrpcServiceTests
{
    private readonly Mock<ICommandService> _commandService;
    private readonly CommandsGrpcService _service;

    public CommandsGrpcServiceTests()
    {
        _commandService = new Mock<ICommandService>();
        _service = new CommandsGrpcService(_commandService.Object, new LoggerFactory().CreateLogger<CommandsGrpcService>());
    }

    [Fact]
    public async Task UpdateCommandStatus_ShouldReturnFailure_WhenTrackingNumberMissing()
    {
        // Arrange
        var request = new UpdateCommandStatusRequest { TrackingNumber = "", KeyCarrier = 1, Action = "delivered" };

        // Act
        var reply = await _service.UpdateCommandStatus(request, TestServerCallContext.Create());

        // Assert
        Assert.False(reply.Success);
        _commandService.Verify(s => s.UpdateCommandStatusByTracking(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCommandStatus_ShouldReturnFailure_WhenKeyCarrierIsZero()
    {
        // Arrange
        var request = new UpdateCommandStatusRequest { TrackingNumber = "TRACK123", KeyCarrier = 0, Action = "delivered" };

        // Act
        var reply = await _service.UpdateCommandStatus(request, TestServerCallContext.Create());

        // Assert
        Assert.False(reply.Success);
        _commandService.Verify(s => s.UpdateCommandStatusByTracking(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCommandStatus_ShouldReturnSuccess_WhenServiceSucceeds()
    {
        // Arrange
        var request = new UpdateCommandStatusRequest { TrackingNumber = "TRACK123", KeyCarrier = 5, Action = "delivered" };
        _commandService.Setup(s => s.UpdateCommandStatusByTracking("TRACK123", 5, "delivered")).Returns(Task.CompletedTask);

        // Act
        var reply = await _service.UpdateCommandStatus(request, TestServerCallContext.Create());

        // Assert
        Assert.True(reply.Success);
        _commandService.Verify(s => s.UpdateCommandStatusByTracking("TRACK123", 5, "delivered"), Times.Once);
    }

    [Fact]
    public async Task UpdateCommandStatus_ShouldReturnFailure_WhenServiceThrows()
    {
        // Arrange
        var request = new UpdateCommandStatusRequest { TrackingNumber = "TRACK123", KeyCarrier = 5, Action = "delivered" };
        _commandService.Setup(s => s.UpdateCommandStatusByTracking(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .ThrowsAsync(new KeyNotFoundException("not found"));

        // Act
        var reply = await _service.UpdateCommandStatus(request, TestServerCallContext.Create());

        // Assert
        Assert.False(reply.Success);
    }
}
