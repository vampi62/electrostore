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

public class KafkaIaStatusConsumerTests
{
    private readonly Mock<IaTrainingGrpc.IaTrainingGrpcClient> _iaTrainingClient = new();
    private readonly Mock<ILogger<KafkaIaStatusConsumer>> _logger = new();

    private KafkaIaStatusConsumer CreateConsumer()
    {
        var configuration = new ConfigurationBuilder().Build();
        return new KafkaIaStatusConsumer(_iaTrainingClient.Object, configuration, _logger.Object);
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
}
