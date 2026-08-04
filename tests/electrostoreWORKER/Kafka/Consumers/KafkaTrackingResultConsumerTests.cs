using System.Reflection;
using Confluent.Kafka;
using ElectrostoreWORKER.Grpc;
using ElectrostoreWORKER.Kafka.Consumers;
using ElectrostoreWORKER.Kafka.Messages;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Metadata = Grpc.Core.Metadata;

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

    private static ConsumeResult<string, string> CreateConsumeResult(string? value, bool isPartitionEOF = false, long offset = 1)
    {
        return new ConsumeResult<string, string>
        {
            Message = value is null ? null : new Message<string, string> { Value = value },
            IsPartitionEOF = isPartitionEOF,
            Offset = offset
        };
    }

    private static Task<ConsumeResult<string, string>?> ConsumeMessageAsync(KafkaTrackingResultConsumer consumer, IConsumer<string, string> kafkaConsumer, CancellationToken ct = default)
    {
        var method = typeof(KafkaTrackingResultConsumer).GetMethod("ConsumeMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ConsumeMessageAsync method not found");
        return (Task<ConsumeResult<string, string>?>)method.Invoke(consumer, new object[] { kafkaConsumer, ct })!;
    }

    private static Task ProcessMessageAsync(KafkaTrackingResultConsumer consumer, IConsumer<string, string> kafkaConsumer, ConsumeResult<string, string> result, CancellationToken ct = default)
    {
        var method = typeof(KafkaTrackingResultConsumer).GetMethod("ProcessMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ProcessMessageAsync method not found");
        return (Task)method.Invoke(consumer, new object[] { kafkaConsumer, result, ct })!;
    }

    private static TrackingResultMessage? DeserializeMessage(KafkaTrackingResultConsumer consumer, ConsumeResult<string, string> result)
    {
        var method = typeof(KafkaTrackingResultConsumer).GetMethod("DeserializeMessage", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("DeserializeMessage method not found");
        return (TrackingResultMessage?)method.Invoke(consumer, new object[] { result });
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

    // ---- DeserializeMessage ----

    [Fact]
    public void DeserializeMessage_ShouldReturnMessage_WhenJsonIsValid()
    {
        // Arrange
        var consumer = CreateConsumer();
        var result = CreateConsumeResult("""{"action":"register","tracking_number":"TN9","carrier":1,"success":true}""");

        // Act
        var msg = DeserializeMessage(consumer, result);

        // Assert
        Assert.NotNull(msg);
        Assert.Equal("TN9", msg!.tracking_number);
    }

    [Fact]
    public void DeserializeMessage_ShouldReturnNull_WhenJsonIsInvalid()
    {
        // Arrange
        var consumer = CreateConsumer();
        var result = CreateConsumeResult("{not-json");

        // Act
        var msg = DeserializeMessage(consumer, result);

        // Assert
        Assert.Null(msg);
    }

    // ---- ConsumeMessageAsync ----

    [Fact]
    public async Task ConsumeMessageAsync_ShouldReturnResult_WhenConsumeSucceeds()
    {
        // Arrange
        var consumer = CreateConsumer();
        var expected = CreateConsumeResult("""{"action":"register","tracking_number":"TN1","carrier":1,"success":true}""");
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        kafkaConsumer.Setup(c => c.Consume(It.IsAny<CancellationToken>())).Returns(expected);

        // Act
        var result = await ConsumeMessageAsync(consumer, kafkaConsumer.Object);

        // Assert
        Assert.Same(expected, result);
    }

    [Fact]
    public async Task ConsumeMessageAsync_ShouldReturnNull_WhenOperationIsCancelled()
    {
        // Arrange
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        kafkaConsumer.Setup(c => c.Consume(It.IsAny<CancellationToken>())).Throws<OperationCanceledException>();

        // Act
        var result = await ConsumeMessageAsync(consumer, kafkaConsumer.Object);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ConsumeMessageAsync_ShouldReturnNull_WhenConsumeExceptionIsThrown()
    {
        // Arrange
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        kafkaConsumer
            .Setup(c => c.Consume(It.IsAny<CancellationToken>()))
            .Throws(new ConsumeException(new ConsumeResult<byte[], byte[]>(), new Error(ErrorCode.UnknownTopicOrPart)));

        // Act
        var result = await ConsumeMessageAsync(consumer, kafkaConsumer.Object);

        // Assert
        Assert.Null(result);
    }

    // ---- ProcessMessageAsync ----

    [Fact]
    public async Task ProcessMessageAsync_ShouldDoNothing_WhenResultIsPartitionEOF()
    {
        // Arrange
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        var result = CreateConsumeResult(null, isPartitionEOF: true);

        // Act
        await ProcessMessageAsync(consumer, kafkaConsumer.Object, result);

        // Assert
        kafkaConsumer.Verify(c => c.Commit(It.IsAny<ConsumeResult<string, string>>()), Times.Never);
        _commandsClient.Verify(c => c.UpdateCommandStatusAsync(It.IsAny<UpdateCommandStatusRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldDoNothing_WhenMessageValueIsNull()
    {
        // Arrange
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        var result = CreateConsumeResult(null);

        // Act
        await ProcessMessageAsync(consumer, kafkaConsumer.Object, result);

        // Assert
        kafkaConsumer.Verify(c => c.Commit(It.IsAny<ConsumeResult<string, string>>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldCommitWithoutDispatching_WhenJsonIsInvalid()
    {
        // Arrange
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        var result = CreateConsumeResult("{not-json");

        // Act
        await ProcessMessageAsync(consumer, kafkaConsumer.Object, result);

        // Assert
        kafkaConsumer.Verify(c => c.Commit(result), Times.Once);
        _commandsClient.Verify(c => c.UpdateCommandStatusAsync(It.IsAny<UpdateCommandStatusRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldDispatchAndCommit_WhenMessageIsValid()
    {
        // Arrange
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        var result = CreateConsumeResult("""{"action":"register","tracking_number":"TN5","carrier":3,"success":true}""");
        _commandsClient
            .Setup(c => c.UpdateCommandStatusAsync(It.IsAny<UpdateCommandStatusRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(new UpdateCommandStatusReply { Success = true }));

        // Act
        await ProcessMessageAsync(consumer, kafkaConsumer.Object, result);

        // Assert
        _commandsClient.Verify(c => c.UpdateCommandStatusAsync(It.Is<UpdateCommandStatusRequest>(r => r.TrackingNumber == "TN5"), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
        kafkaConsumer.Verify(c => c.Commit(result), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldNotCommit_WhenTrackingFailed()
    {
        // Arrange - DispatchAsync returns false when the tracking event itself reports failure
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        var result = CreateConsumeResult("""{"action":"register","tracking_number":"TN6","carrier":3,"success":false,"error_code":5}""");

        // Act
        await ProcessMessageAsync(consumer, kafkaConsumer.Object, result);

        // Assert
        kafkaConsumer.Verify(c => c.Commit(It.IsAny<ConsumeResult<string, string>>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldNotCommitOrThrow_WhenDispatchThrows()
    {
        // Arrange
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        var result = CreateConsumeResult("""{"action":"register","tracking_number":"TN7","carrier":3,"success":true}""");
        _commandsClient
            .Setup(c => c.UpdateCommandStatusAsync(It.IsAny<UpdateCommandStatusRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Throws(new RpcException(new Status(StatusCode.Unavailable, "down")));

        // Act
        var exception = await Record.ExceptionAsync(() => ProcessMessageAsync(consumer, kafkaConsumer.Object, result));

        // Assert
        Assert.Null(exception);
        kafkaConsumer.Verify(c => c.Commit(It.IsAny<ConsumeResult<string, string>>()), Times.Never);
    }
}
