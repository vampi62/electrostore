using System.Reflection;
using Confluent.Kafka;
using ElectrostoreAPI.Kafka.Producer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElectrostoreAPI.Tests.Kafka;

public class KafkaProducerServiceTests
{
    // Nothing listens here, so the real producer built by the constructor never reaches a broker.
    private const string UnreachableBroker = "127.0.0.1:1";

    private readonly Mock<ILogger<KafkaProducerService>> _logger = new();

    private static IConfiguration BuildConfiguration(string? bootstrapServers = UnreachableBroker)
    {
        var values = new Dictionary<string, string?>();
        if (bootstrapServers is not null)
        {
            values["Kafka:BootstrapServers"] = bootstrapServers;
        }
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    // Swaps the real librdkafka producer for a mock so that PublishAsync/Dispose can be exercised without a broker.
    private static Mock<IProducer<string, string>> ReplaceProducerWithMock(KafkaProducerService service)
    {
        var field = typeof(KafkaProducerService).GetField("_producer", BindingFlags.Instance | BindingFlags.NonPublic)!;
        ((IProducer<string, string>)field.GetValue(service)!).Dispose();
        var mock = new Mock<IProducer<string, string>>();
        field.SetValue(service, mock.Object);
        return mock;
    }

    private static bool LoggedAt(Mock<ILogger<KafkaProducerService>> logger, LogLevel level) =>
        logger.Invocations.Any(i => i.Method.Name == nameof(ILogger.Log) && (LogLevel)i.Arguments[0] == level);

    // --- PublishAsync ---

    [Fact]
    public async Task PublishAsync_ShouldProduceMessageWithKeyAndValue()
    {
        var service = new KafkaProducerService(BuildConfiguration(), _logger.Object);
        var producer = ReplaceProducerWithMock(service);
        producer
            .Setup(p => p.ProduceAsync("topic-a", It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryResult<string, string> { Partition = new Partition(1), Offset = new Offset(5) });

        await service.PublishAsync("topic-a", "key-1", "payload");

        producer.Verify(p => p.ProduceAsync(
            "topic-a",
            It.Is<Message<string, string>>(m => m.Key == "key-1" && m.Value == "payload"),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(LoggedAt(_logger, LogLevel.Error));
    }

    [Fact]
    public async Task PublishAsync_ShouldForwardCancellationToken()
    {
        var service = new KafkaProducerService(BuildConfiguration(), _logger.Object);
        var producer = ReplaceProducerWithMock(service);
        producer
            .Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryResult<string, string>());
        using var cts = new CancellationTokenSource();

        await service.PublishAsync("t", "k", "m", cts.Token);

        producer.Verify(p => p.ProduceAsync("t", It.IsAny<Message<string, string>>(), cts.Token), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ShouldSwallowAndLogProduceException()
    {
        var service = new KafkaProducerService(BuildConfiguration(), _logger.Object);
        var producer = ReplaceProducerWithMock(service);
        producer
            .Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ProduceException<string, string>(new Error(ErrorCode.Local_MsgTimedOut, "timed out"), new DeliveryResult<string, string>()));

        var exception = await Record.ExceptionAsync(() => service.PublishAsync("t", "k", "m"));

        Assert.Null(exception);
        Assert.True(LoggedAt(_logger, LogLevel.Error));
    }

    [Fact]
    public async Task PublishAsync_ShouldPropagateOtherExceptions()
    {
        var service = new KafkaProducerService(BuildConfiguration(), _logger.Object);
        var producer = ReplaceProducerWithMock(service);
        producer
            .Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.PublishAsync("t", "k", "m"));
    }

    // --- IsConnectedAsync ---

    [Fact]
    public async Task IsConnectedAsync_ShouldReturnFalse_WhenBrokerIsUnreachable()
    {
        using var service = new KafkaProducerService(BuildConfiguration(), _logger.Object);

        var connected = await service.IsConnectedAsync();

        Assert.False(connected);
    }

    // --- Constructor / Dispose ---

    [Fact]
    public void Constructor_ShouldFallBackToDefaultBootstrapServers_WhenNotConfigured()
    {
        using var service = new KafkaProducerService(BuildConfiguration(bootstrapServers: null), _logger.Object);

        var field = typeof(KafkaProducerService).GetField("_bootstrapServers", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assert.Equal("kafka:9092", field.GetValue(service));
    }

    [Fact]
    public void Constructor_ShouldUseConfiguredBootstrapServers()
    {
        using var service = new KafkaProducerService(BuildConfiguration("broker-1:9092"), _logger.Object);

        var field = typeof(KafkaProducerService).GetField("_bootstrapServers", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assert.Equal("broker-1:9092", field.GetValue(service));
    }

    [Fact]
    public void Dispose_ShouldFlushThenDisposeProducer()
    {
        var service = new KafkaProducerService(BuildConfiguration(), _logger.Object);
        var producer = ReplaceProducerWithMock(service);

        service.Dispose();

        producer.Verify(p => p.Flush(It.IsAny<TimeSpan>()), Times.Once);
        producer.Verify(p => p.Dispose(), Times.Once);
    }
}
