using ElectrostoreCRON.Kafka.Producer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElectrostoreCRON.Tests.Kafka.Producer;

public class KafkaProducerServiceTests
{
    // The constructor builds a real Confluent.Kafka IProducer (not injected/mockable), and
    // PublishAsync/ProduceAsync would either block for the full librdkafka message-timeout
    // (minutes) or behave non-deterministically without a reachable broker, so only the
    // construction and disposal lifecycle - which don't require connectivity - are unit-tested
    // here.
    private static KafkaProducerService CreateService(Dictionary<string, string?>? values = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values ?? []).Build();
        return new KafkaProducerService(configuration, new Mock<ILogger<KafkaProducerService>>().Object);
    }

    [Fact]
    public void Constructor_ShouldSucceed_WhenBootstrapServersIsMissing()
    {
        // Act
        var exception = Record.Exception(() => CreateService());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_ShouldSucceed_WhenBootstrapServersIsProvided()
    {
        // Act
        var exception = Record.Exception(() => CreateService(new Dictionary<string, string?>
        {
            ["Kafka:BootstrapServers"] = "127.0.0.1:1"
        }));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_ShouldNotThrow_WhenNoMessageWasPublished()
    {
        // Arrange
        var service = CreateService();

        // Act
        var exception = Record.Exception(service.Dispose);

        // Assert
        Assert.Null(exception);
    }
}
