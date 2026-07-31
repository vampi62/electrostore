using System.Reflection;
using ElectrostoreWORKER.Grpc;
using ElectrostoreWORKER.Kafka.Consumers;
using ElectrostoreWORKER.Kafka.Messages;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElectrostoreWORKER.Tests.Kafka.Consumers;

public class KafkaTrackingResultConsumerTests
{
    private readonly Mock<CommandsGrpc.CommandsGrpcClient> _commandsClient = new();
    private readonly Mock<ILogger<KafkaTrackingResultConsumer>> _logger = new();

    private KafkaTrackingResultConsumer CreateConsumer()
    {
        var configuration = new ConfigurationBuilder().Build();
        return new KafkaTrackingResultConsumer(_commandsClient.Object, configuration, _logger.Object);
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

    private static Task DispatchAsync(KafkaTrackingResultConsumer consumer, TrackingResultMessage message, CancellationToken ct = default)
    {
        var method = typeof(KafkaTrackingResultConsumer).GetMethod("DispatchAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("DispatchAsync method not found");
        return (Task)method.Invoke(consumer, new object[] { message, ct })!;
    }

    [Fact]
    public async Task DispatchAsync_ShouldNotCallApi_WhenTrackingFailed()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = new TrackingResultMessage
        {
            action = "register",
            tracking_number = "TN1",
            carrier = 1,
            success = false,
            error_code = 42
        };

        // Act
        await DispatchAsync(consumer, message);

        // Assert
        _commandsClient.Verify(c => c.UpdateCommandStatusAsync(It.IsAny<UpdateCommandStatusRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("register")]
    [InlineData("stoptrack")]
    [InlineData("retrack")]
    [InlineData("deletetrack")]
    public async Task DispatchAsync_ShouldUpdateCommandStatus_ForStatusUpdatingActions(string action)
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = new TrackingResultMessage
        {
            action = action,
            tracking_number = "TN1",
            carrier = 5,
            success = true,
            error_code = null
        };
        _commandsClient
            .Setup(c => c.UpdateCommandStatusAsync(It.IsAny<UpdateCommandStatusRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(new UpdateCommandStatusReply { Success = true }));

        // Act
        await DispatchAsync(consumer, message);

        // Assert
        _commandsClient.Verify(c => c.UpdateCommandStatusAsync(
            It.Is<UpdateCommandStatusRequest>(r =>
                r.KeyCarrier == 5 &&
                r.TrackingNumber == "TN1" &&
                r.Action == action &&
                r.Success &&
                r.ErrorCode == 0),
            It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_ShouldPassThroughErrorCode_WhenProvided()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = new TrackingResultMessage
        {
            action = "register",
            tracking_number = "TN2",
            carrier = 2,
            success = true,
            error_code = 7
        };
        _commandsClient
            .Setup(c => c.UpdateCommandStatusAsync(It.IsAny<UpdateCommandStatusRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(new UpdateCommandStatusReply { Success = true }));

        // Act
        await DispatchAsync(consumer, message);

        // Assert
        _commandsClient.Verify(c => c.UpdateCommandStatusAsync(
            It.Is<UpdateCommandStatusRequest>(r => r.ErrorCode == 7),
            It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("changecarrier")]
    [InlineData("changeinfo")]
    [InlineData("push")]
    [InlineData("unknown-action")]
    public async Task DispatchAsync_ShouldNotCallApi_ForNonStatusUpdatingActions(string action)
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = new TrackingResultMessage
        {
            action = action,
            tracking_number = "TN3",
            carrier = 1,
            success = true
        };

        // Act
        var exception = await Record.ExceptionAsync(() => DispatchAsync(consumer, message));

        // Assert
        Assert.Null(exception);
        _commandsClient.Verify(c => c.UpdateCommandStatusAsync(It.IsAny<UpdateCommandStatusRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
