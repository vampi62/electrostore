using System.Reflection;
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
}
