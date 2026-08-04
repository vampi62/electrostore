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

public class KafkaIaStatusConsumerTests
{
    private readonly Mock<IaTrainingGrpc.IaTrainingGrpcClient> _iaTrainingClient = new();
    private readonly Mock<ILogger<KafkaIaStatusConsumer>> _logger = new();

    private KafkaIaStatusConsumer CreateConsumer()
    {
        var configuration = new ConfigurationBuilder().Build();
        return new KafkaIaStatusConsumer(_iaTrainingClient.Object, configuration, _logger.Object);
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

    private static Task<ConsumeResult<string, string>?> ConsumeMessageAsync(KafkaIaStatusConsumer consumer, IConsumer<string, string> kafkaConsumer, CancellationToken ct = default)
    {
        var method = typeof(KafkaIaStatusConsumer).GetMethod("ConsumeMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ConsumeMessageAsync method not found");
        return (Task<ConsumeResult<string, string>?>)method.Invoke(consumer, new object[] { kafkaConsumer, ct })!;
    }

    private static Task ProcessMessageAsync(KafkaIaStatusConsumer consumer, IConsumer<string, string> kafkaConsumer, ConsumeResult<string, string> result, CancellationToken ct = default)
    {
        var method = typeof(KafkaIaStatusConsumer).GetMethod("ProcessMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ProcessMessageAsync method not found");
        return (Task)method.Invoke(consumer, new object[] { kafkaConsumer, result, ct })!;
    }

    private static IaStatusMessage? DeserializeMessage(KafkaIaStatusConsumer consumer, ConsumeResult<string, string> result)
    {
        var method = typeof(KafkaIaStatusConsumer).GetMethod("DeserializeMessage", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("DeserializeMessage method not found");
        return (IaStatusMessage?)method.Invoke(consumer, new object[] { result });
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

    private static Task DispatchAsync(KafkaIaStatusConsumer consumer, IaStatusMessage message, CancellationToken ct = default)
    {
        var method = typeof(KafkaIaStatusConsumer).GetMethod("DispatchAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("DispatchAsync method not found");
        return (Task)method.Invoke(consumer, new object[] { message, ct })!;
    }

    [Fact]
    public async Task DispatchAsync_ShouldMapAllFields_ToUpdateIaStatusRequest()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = new IaStatusMessage
        {
            action = "training_completed",
            id_ia = 12,
            message = "done",
            requested_by = 3,
            accuracy = 0.9f,
            val_accuracy = 0.85f,
            loss = 0.1f,
            val_loss = 0.15f,
            epoch = 20
        };
        _iaTrainingClient
            .Setup(c => c.UpdateIaStatusAsync(It.IsAny<UpdateIaStatusRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(new UpdateIaStatusReply { Success = true }));

        // Act
        await DispatchAsync(consumer, message);

        // Assert
        _iaTrainingClient.Verify(c => c.UpdateIaStatusAsync(
            It.Is<UpdateIaStatusRequest>(r =>
                r.IdIa == 12 &&
                r.Action == "training_completed" &&
                r.RequestedBy == 3 &&
                r.Message == "done" &&
                r.Accuracy == 0.9f &&
                r.ValAccuracy == 0.85f &&
                r.Loss == 0.1f &&
                r.ValLoss == 0.15f &&
                r.Epoch == 20),
            It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_ShouldNotThrow_WhenApiRejectsUpdate()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = new IaStatusMessage { action = "training_failed", id_ia = 1 };
        _iaTrainingClient
            .Setup(c => c.UpdateIaStatusAsync(It.IsAny<UpdateIaStatusRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(new UpdateIaStatusReply { Success = false }));

        // Act
        var exception = await Record.ExceptionAsync(() => DispatchAsync(consumer, message));

        // Assert
        Assert.Null(exception);
    }

    // ---- DeserializeMessage ----

    [Fact]
    public void DeserializeMessage_ShouldReturnMessage_WhenJsonIsValid()
    {
        // Arrange
        var consumer = CreateConsumer();
        var result = CreateConsumeResult("""{"action":"training_completed","id_ia":7}""");

        // Act
        var msg = DeserializeMessage(consumer, result);

        // Assert
        Assert.NotNull(msg);
        Assert.Equal("training_completed", msg!.action);
        Assert.Equal(7, msg.id_ia);
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
        var expected = CreateConsumeResult("""{"action":"training_completed","id_ia":1}""");
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
        _iaTrainingClient.Verify(c => c.UpdateIaStatusAsync(It.IsAny<UpdateIaStatusRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
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
        _iaTrainingClient.Verify(c => c.UpdateIaStatusAsync(It.IsAny<UpdateIaStatusRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldDispatchAndCommit_WhenMessageIsValid()
    {
        // Arrange
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        var result = CreateConsumeResult("""{"action":"training_completed","id_ia":9}""");
        _iaTrainingClient
            .Setup(c => c.UpdateIaStatusAsync(It.IsAny<UpdateIaStatusRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(new UpdateIaStatusReply { Success = true }));

        // Act
        await ProcessMessageAsync(consumer, kafkaConsumer.Object, result);

        // Assert
        _iaTrainingClient.Verify(c => c.UpdateIaStatusAsync(It.Is<UpdateIaStatusRequest>(r => r.IdIa == 9), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
        kafkaConsumer.Verify(c => c.Commit(result), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldNotCommitOrThrow_WhenDispatchThrows()
    {
        // Arrange
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        var result = CreateConsumeResult("""{"action":"training_completed","id_ia":9}""");
        _iaTrainingClient
            .Setup(c => c.UpdateIaStatusAsync(It.IsAny<UpdateIaStatusRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Throws(new RpcException(new Status(StatusCode.Unavailable, "down")));

        // Act
        var exception = await Record.ExceptionAsync(() => ProcessMessageAsync(consumer, kafkaConsumer.Object, result));

        // Assert
        Assert.Null(exception);
        kafkaConsumer.Verify(c => c.Commit(It.IsAny<ConsumeResult<string, string>>()), Times.Never);
    }
}
