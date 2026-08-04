using System.Reflection;
using Confluent.Kafka;
using ElectrostoreWORKER.Kafka.Consumers;
using ElectrostoreWORKER.Kafka.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElectrostoreWORKER.Tests.Kafka.Consumers;

public class KafkaMqttUserConsumerTests
{
    // KafkaMqttUserConsumer talks to the Mosquitto container through a real Docker.DotNet client
    // that isn't injected/mockable, so only the validation branch that returns before touching
    // Docker can be safely unit-tested here.
    private readonly Mock<ILogger<KafkaMqttUserConsumer>> _logger = new();

    private KafkaMqttUserConsumer CreateConsumer()
    {
        var configuration = new ConfigurationBuilder().Build();
        return new KafkaMqttUserConsumer(configuration, _logger.Object);
    }

    private static Task DispatchAsync(KafkaMqttUserConsumer consumer, MqttUserMessage message, CancellationToken ct = default)
    {
        var method = typeof(KafkaMqttUserConsumer).GetMethod("DispatchAsync", BindingFlags.NonPublic | BindingFlags.Instance)
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

    private static Task<ConsumeResult<string, string>?> ConsumeMessageAsync(KafkaMqttUserConsumer consumer, IConsumer<string, string> kafkaConsumer, CancellationToken ct = default)
    {
        var method = typeof(KafkaMqttUserConsumer).GetMethod("ConsumeMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ConsumeMessageAsync method not found");
        return (Task<ConsumeResult<string, string>?>)method.Invoke(consumer, new object[] { kafkaConsumer, ct })!;
    }

    private static Task ProcessMessageAsync(KafkaMqttUserConsumer consumer, IConsumer<string, string> kafkaConsumer, ConsumeResult<string, string> result, CancellationToken ct = default)
    {
        var method = typeof(KafkaMqttUserConsumer).GetMethod("ProcessMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ProcessMessageAsync method not found");
        return (Task)method.Invoke(consumer, new object[] { kafkaConsumer, result, ct })!;
    }

    private static MqttUserMessage? DeserializeMessage(KafkaMqttUserConsumer consumer, ConsumeResult<string, string> result)
    {
        var method = typeof(KafkaMqttUserConsumer).GetMethod("DeserializeMessage", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("DeserializeMessage method not found");
        return (MqttUserMessage?)method.Invoke(consumer, new object[] { result });
    }

    [Fact]
    public async Task DispatchAsync_ShouldReturnWithoutError_WhenUserIsMissing()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = new MqttUserMessage { user = null, password = "secret", delete = false };

        // Act
        var exception = await Record.ExceptionAsync(() => DispatchAsync(consumer, message));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task DispatchAsync_ShouldReturnWithoutError_WhenPasswordIsMissing()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = new MqttUserMessage { user = "alice", password = "", delete = false };

        // Act
        var exception = await Record.ExceptionAsync(() => DispatchAsync(consumer, message));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task DispatchAsync_ShouldReturnWithoutError_WhenDeleteIsFalseAndUserAndPasswordAreWhitespace()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = new MqttUserMessage { user = "   ", password = "   ", delete = null };

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
        var result = CreateConsumeResult("""{"user":"alice","password":"secret"}""");

        // Act
        var msg = DeserializeMessage(consumer, result);

        // Assert
        Assert.NotNull(msg);
        Assert.Equal("alice", msg!.user);
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
        var expected = CreateConsumeResult("""{"user":"alice","password":"secret"}""");
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
    // Only scenarios that do not require a valid/successful dispatch are exercised here,
    // since a successful dispatch would reach the real, unmockable Docker client.

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
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldNotCommit_WhenMessageFailsValidation()
    {
        // Arrange - missing password fails validation before touching Docker, DispatchAsync returns false
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        var result = CreateConsumeResult("""{"user":"alice","password":""}""");

        // Act
        await ProcessMessageAsync(consumer, kafkaConsumer.Object, result);

        // Assert
        kafkaConsumer.Verify(c => c.Commit(It.IsAny<ConsumeResult<string, string>>()), Times.Never);
    }
}
