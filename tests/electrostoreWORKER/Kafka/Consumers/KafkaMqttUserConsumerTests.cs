using System.Reflection;
using Confluent.Kafka;
using Docker.DotNet;
using Docker.DotNet.Models;
using ElectrostoreWORKER.Kafka.Consumers;
using ElectrostoreWORKER.Kafka.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElectrostoreWORKER.Tests.Kafka.Consumers;

public class KafkaMqttUserConsumerTests
{
    private const string MosquittoContainerId = "mosquitto-container-id";

    private readonly Mock<ILogger<KafkaMqttUserConsumer>> _logger = new();
    private readonly Mock<IDockerClient> _dockerClient = new();
    private readonly Mock<IContainerOperations> _containerOperations = new();
    private readonly Mock<IExecOperations> _execOperations = new();

    public KafkaMqttUserConsumerTests()
    {
        _dockerClient.Setup(d => d.Containers).Returns(_containerOperations.Object);
        _dockerClient.Setup(d => d.Exec).Returns(_execOperations.Object);
    }

    private KafkaMqttUserConsumer CreateConsumer()
    {
        var configuration = new ConfigurationBuilder().Build();
        return new KafkaMqttUserConsumer(configuration, _logger.Object, _dockerClient.Object);
    }

    private void SetupMosquittoContainerFound()
    {
        _containerOperations
            .Setup(c => c.ListContainersAsync(It.IsAny<ContainersListParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ContainerListResponse>
            {
                new() { ID = MosquittoContainerId, Names = new List<string> { "/electrostore-mqtt" } }
            });
    }

    private void SetupMosquittoContainerNotFound()
    {
        _containerOperations
            .Setup(c => c.ListContainersAsync(It.IsAny<ContainersListParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ContainerListResponse>());
    }

    private void SetupExecSucceeds()
    {
        _execOperations
            .Setup(e => e.ExecCreateContainerAsync(MosquittoContainerId, It.IsAny<ContainerExecCreateParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContainerExecCreateResponse { ID = "exec-id" });
        _execOperations
            .Setup(e => e.StartContainerExecAsync("exec-id", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private static Task<bool> DispatchAsync(KafkaMqttUserConsumer consumer, MqttUserMessage message, CancellationToken ct = default)
    {
        var method = typeof(KafkaMqttUserConsumer).GetMethod("DispatchAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("DispatchAsync method not found");
        return (Task<bool>)method.Invoke(consumer, new object[] { message, ct })!;
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

    // ---- DispatchAsync (Docker interactions, via mocked IDockerClient) ----

    [Fact]
    public async Task DispatchAsync_ShouldDeleteUserAndReload_WhenDeleteIsTrue()
    {
        // Arrange
        SetupMosquittoContainerFound();
        SetupExecSucceeds();
        var consumer = CreateConsumer();
        var message = new MqttUserMessage { user = "alice", delete = true };

        // Act
        var dispatched = await DispatchAsync(consumer, message);

        // Assert
        Assert.True(dispatched);
        _execOperations.Verify(e => e.ExecCreateContainerAsync(
            MosquittoContainerId,
            It.Is<ContainerExecCreateParameters>(p => string.Join(" ", p.Cmd!).Contains("mosquitto_passwd -D") && string.Join(" ", p.Cmd!).Contains("alice")),
            It.IsAny<CancellationToken>()), Times.Once);
        _execOperations.Verify(e => e.ExecCreateContainerAsync(
            MosquittoContainerId,
            It.Is<ContainerExecCreateParameters>(p => string.Join(" ", p.Cmd!).Contains("kill -HUP 1")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_ShouldRenameThenAddUserAndReload_WhenOldUserDiffersFromUser()
    {
        // Arrange
        SetupMosquittoContainerFound();
        SetupExecSucceeds();
        var consumer = CreateConsumer();
        var message = new MqttUserMessage { user = "bob", old_user = "alice", password = "secret", delete = false };

        // Act
        var dispatched = await DispatchAsync(consumer, message);

        // Assert
        Assert.True(dispatched);
        _execOperations.Verify(e => e.ExecCreateContainerAsync(
            MosquittoContainerId,
            It.Is<ContainerExecCreateParameters>(p => string.Join(" ", p.Cmd!).Contains("mosquitto_passwd -D") && string.Join(" ", p.Cmd!).Contains("alice")),
            It.IsAny<CancellationToken>()), Times.Once);
        _execOperations.Verify(e => e.ExecCreateContainerAsync(
            MosquittoContainerId,
            It.Is<ContainerExecCreateParameters>(p => string.Join(" ", p.Cmd!).Contains("mosquitto_passwd -b") && string.Join(" ", p.Cmd!).Contains("bob") && string.Join(" ", p.Cmd!).Contains("secret")),
            It.IsAny<CancellationToken>()), Times.Once);
        _execOperations.Verify(e => e.ExecCreateContainerAsync(
            MosquittoContainerId,
            It.Is<ContainerExecCreateParameters>(p => string.Join(" ", p.Cmd!).Contains("kill -HUP 1")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_ShouldAddUserAndReload_WhenNoOldUserProvided()
    {
        // Arrange
        SetupMosquittoContainerFound();
        SetupExecSucceeds();
        var consumer = CreateConsumer();
        var message = new MqttUserMessage { user = "carol", password = "secret", delete = false };

        // Act
        var dispatched = await DispatchAsync(consumer, message);

        // Assert
        Assert.True(dispatched);
        _execOperations.Verify(e => e.ExecCreateContainerAsync(
            It.IsAny<string>(), It.IsAny<ContainerExecCreateParameters>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task DispatchAsync_ShouldReturnTrue_WhenMosquittoContainerIsNotFound()
    {
        // Arrange - ExecuteCommandInMosquittoAsync fails internally but DispatchAsync still reports success
        SetupMosquittoContainerNotFound();
        var consumer = CreateConsumer();
        var message = new MqttUserMessage { user = "carol", password = "secret", delete = false };

        // Act
        var dispatched = await DispatchAsync(consumer, message);

        // Assert
        Assert.True(dispatched);
        _execOperations.Verify(e => e.ExecCreateContainerAsync(
            It.IsAny<string>(), It.IsAny<ContainerExecCreateParameters>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_ShouldReturnTrue_WhenDockerExecThrows()
    {
        // Arrange
        SetupMosquittoContainerFound();
        _execOperations
            .Setup(e => e.ExecCreateContainerAsync(MosquittoContainerId, It.IsAny<ContainerExecCreateParameters>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("docker exec failed"));
        var consumer = CreateConsumer();
        var message = new MqttUserMessage { user = "carol", password = "secret", delete = false };

        // Act
        var dispatched = await DispatchAsync(consumer, message);

        // Assert
        Assert.True(dispatched);
    }

    // ---- ProcessMessageAsync (end-to-end through DispatchAsync, via mocked IDockerClient) ----

    [Fact]
    public async Task ProcessMessageAsync_ShouldCommit_WhenDispatchSucceeds()
    {
        // Arrange
        SetupMosquittoContainerFound();
        SetupExecSucceeds();
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        var result = CreateConsumeResult("""{"user":"carol","password":"secret"}""");

        // Act
        await ProcessMessageAsync(consumer, kafkaConsumer.Object, result);

        // Assert
        kafkaConsumer.Verify(c => c.Commit(result), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldLogAndNotCommit_WhenDispatchThrows()
    {
        // Arrange - the Docker container listing call is outside DispatchAsync's own try/catch,
        // so an unexpected failure there surfaces as an exception ProcessMessageAsync must catch.
        _containerOperations
            .Setup(c => c.ListContainersAsync(It.IsAny<ContainersListParameters>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("docker unreachable"));
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        var result = CreateConsumeResult("""{"user":"carol","password":"secret"}""");

        // Act
        var exception = await Record.ExceptionAsync(() => ProcessMessageAsync(consumer, kafkaConsumer.Object, result));

        // Assert
        Assert.Null(exception);
        kafkaConsumer.Verify(c => c.Commit(It.IsAny<ConsumeResult<string, string>>()), Times.Never);
    }

    // ---- ExecuteAsync ----

    private sealed class TestableKafkaMqttUserConsumer(
        IConfiguration configuration,
        ILogger<KafkaMqttUserConsumer> logger,
        IDockerClient dockerClient,
        IConsumer<string, string> consumerToUse) : KafkaMqttUserConsumer(configuration, logger, dockerClient)
    {
        protected override IConsumer<string, string> BuildConsumer(ConsumerConfig config) => consumerToUse;

        public Task RunExecuteAsync(CancellationToken ct) => ExecuteAsync(ct);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldConsumeMessagesThenStop_WhenCancelled()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        using var cts = new CancellationTokenSource();
        var callCount = 0;
        kafkaConsumer
            .Setup(c => c.Consume(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return CreateConsumeResult(null, isPartitionEOF: true);
                }
                cts.Cancel();
                throw new OperationCanceledException();
            });
        var consumer = new TestableKafkaMqttUserConsumer(configuration, _logger.Object, _dockerClient.Object, kafkaConsumer.Object);

        // Act
        await consumer.RunExecuteAsync(cts.Token);

        // Assert
        kafkaConsumer.Verify(c => c.Subscribe("mqtt-user-events"), Times.Once);
        kafkaConsumer.Verify(c => c.Consume(It.IsAny<CancellationToken>()), Times.Exactly(2));
        kafkaConsumer.Verify(c => c.Close(), Times.Once);
    }
}
