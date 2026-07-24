using System.Reflection;
using ElectrostoreIA.Dto;
using ElectrostoreIA.Enums;
using ElectrostoreIA.Kafka.Consumers;
using ElectrostoreIA.Kafka.Producer;
using ElectrostoreIA.Services.ModelTrainerService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElectrostoreIA.Tests.Kafka.Consumers;

public class KafkaIaTrainConsumerTests
{
    private readonly Mock<IModelTrainerService> _trainerService = new();
    private readonly Mock<IKafkaProducerService> _kafkaProducer = new();
    private readonly Mock<ILogger<KafkaIaTrainConsumer>> _logger = new();

    // TrainRequestMessage is an internal type with no InternalsVisibleTo grant to the test
    // assembly, so it is built and passed to the private DispatchAsync method entirely via reflection.
    private static readonly Type TrainRequestMessageType =
        typeof(KafkaIaTrainConsumer).Assembly.GetType("ElectrostoreIA.Dto.TrainRequestMessage")
        ?? throw new InvalidOperationException("TrainRequestMessage type not found");

    private KafkaIaTrainConsumer CreateConsumer()
    {
        var configuration = new ConfigurationBuilder().Build();
        return new KafkaIaTrainConsumer(_trainerService.Object, _kafkaProducer.Object, configuration, _logger.Object);
    }

    private static object CreateTrainRequestMessage(string action, int idIa, int requestedBy = 0)
    {
        var msg = Activator.CreateInstance(TrainRequestMessageType)!;
        TrainRequestMessageType.GetProperty("action")!.SetValue(msg, action);
        TrainRequestMessageType.GetProperty("id_ia")!.SetValue(msg, idIa);
        TrainRequestMessageType.GetProperty("requested_by")!.SetValue(msg, requestedBy);
        TrainRequestMessageType.GetProperty("requested_at")!.SetValue(msg, DateTime.UtcNow);
        return msg;
    }

    private static Task DispatchAsync(KafkaIaTrainConsumer consumer, object message, CancellationToken ct = default)
    {
        var method = typeof(KafkaIaTrainConsumer).GetMethod("DispatchAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("DispatchAsync method not found");
        return (Task)method.Invoke(consumer, new object[] { message, ct })!;
    }

    [Fact]
    public async Task DispatchAsync_ShouldDoNothing_WhenActionIsUnknown()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = CreateTrainRequestMessage("unknown-action", idIa: 1);

        // Act
        await DispatchAsync(consumer, message);

        // Assert
        _trainerService.Verify(t => t.DeleteModelFilesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _trainerService.Verify(t => t.StartAndWaitAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _kafkaProducer.Verify(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- ia_deleted ----

    [Fact]
    public async Task DispatchAsync_ShouldDeleteModelFiles_WhenActionIsIaDeleted()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = CreateTrainRequestMessage("ia_deleted", idIa: 10);
        _trainerService.Setup(t => t.DeleteModelFilesAsync(10, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await DispatchAsync(consumer, message);

        // Assert
        _trainerService.Verify(t => t.DeleteModelFilesAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        _kafkaProducer.Verify(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_ShouldNotThrow_WhenDeleteModelFilesFails()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = CreateTrainRequestMessage("ia_deleted", idIa: 11);
        _trainerService.Setup(t => t.DeleteModelFilesAsync(11, It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("disk error"));

        // Act
        var exception = await Record.ExceptionAsync(() => DispatchAsync(consumer, message));

        // Assert
        Assert.Null(exception);
    }

    // ---- train_requested ----

    [Fact]
    public async Task DispatchAsync_ShouldPublishStartedThenCompletedStatus_WhenTrainingSucceeds()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = CreateTrainRequestMessage("train_requested", idIa: 20, requestedBy: 5);
        _trainerService.Setup(t => t.StartAndWaitAsync(20, 5, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _trainerService.Setup(t => t.GetTrainingStatus(20)).Returns(new TrainingProgress
        {
            Status = TrainingStatus.Completed,
            Message = "Training completed",
            Accuracy = 0.95f,
            RequestedBy = 5
        });
        var publishedMessages = new List<(string topic, string key, string value)>();
        _kafkaProducer
            .Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((topic, key, value, _) => publishedMessages.Add((topic, key, value)))
            .Returns(Task.CompletedTask);

        // Act
        await DispatchAsync(consumer, message);

        // Assert
        _trainerService.Verify(t => t.SetTrainingProgressMap(20, It.Is<TrainingProgress>(p => p.Status == TrainingStatus.InWaiting)), Times.Once);
        Assert.Equal(2, publishedMessages.Count);
        Assert.All(publishedMessages, m => Assert.Equal("ia-status", m.topic));
        Assert.All(publishedMessages, m => Assert.Equal("20", m.key));
        Assert.Contains("training_started", publishedMessages[0].value);
        Assert.Contains("training_completed", publishedMessages[1].value);
    }

    [Fact]
    public async Task DispatchAsync_ShouldPublishFailedStatus_WhenTrainingReturnsFalse()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = CreateTrainRequestMessage("train_requested", idIa: 21, requestedBy: 6);
        _trainerService.Setup(t => t.StartAndWaitAsync(21, 6, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _trainerService.Setup(t => t.GetTrainingStatus(21)).Returns(new TrainingProgress
        {
            Status = TrainingStatus.Error,
            Message = "No training images found",
            RequestedBy = 6
        });
        var publishedMessages = new List<string>();
        _kafkaProducer
            .Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, _, value, _) => publishedMessages.Add(value))
            .Returns(Task.CompletedTask);

        // Act
        await DispatchAsync(consumer, message);

        // Assert
        Assert.Equal(2, publishedMessages.Count);
        Assert.Contains("training_failed", publishedMessages[1]);
        Assert.Contains("No training images found", publishedMessages[1]);
    }

    [Fact]
    public async Task DispatchAsync_ShouldPublishFailedStatus_WithExceptionMessage_WhenStartAndWaitAsyncThrows()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = CreateTrainRequestMessage("train_requested", idIa: 22, requestedBy: 7);
        _trainerService.Setup(t => t.StartAndWaitAsync(22, 7, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("trainer exploded"));
        _trainerService.Setup(t => t.GetTrainingStatus(22)).Returns((TrainingProgress?)null);
        var publishedMessages = new List<string>();
        _kafkaProducer
            .Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, _, value, _) => publishedMessages.Add(value))
            .Returns(Task.CompletedTask);

        // Act
        var exception = await Record.ExceptionAsync(() => DispatchAsync(consumer, message));

        // Assert
        Assert.Null(exception);
        Assert.Equal(2, publishedMessages.Count);
        Assert.Contains("training_failed", publishedMessages[1]);
        Assert.Contains("trainer exploded", publishedMessages[1]);
    }

    [Fact]
    public async Task DispatchAsync_WhenTrainingIsCancelled_ThrowsSemaphoreFullException()
    {
        // Documents a latent double-release bug: the OperationCanceledException catch block
        // explicitly calls _trainingSemaphore.Release() and then returns, but the surrounding
        // finally block releases the same (1,1) semaphore a second time on the way out,
        // overflowing it. If this is ever fixed, this test's expected exception should become null.
        var consumer = CreateConsumer();
        var message = CreateTrainRequestMessage("train_requested", idIa: 25, requestedBy: 1);
        _trainerService.Setup(t => t.StartAndWaitAsync(25, 1, It.IsAny<CancellationToken>())).ThrowsAsync(new OperationCanceledException());
        _kafkaProducer
            .Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var exception = await Record.ExceptionAsync(() => DispatchAsync(consumer, message));

        Assert.IsType<SemaphoreFullException>(exception);
    }

    [Fact]
    public async Task DispatchAsync_ShouldReleaseSemaphore_AllowingSubsequentTrainingRequests()
    {
        // Arrange - two sequential train_requested dispatches must both complete, proving the
        // semaphore that serializes training is released after each run.
        var consumer = CreateConsumer();
        _trainerService.Setup(t => t.StartAndWaitAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _trainerService.Setup(t => t.GetTrainingStatus(It.IsAny<int>())).Returns(new TrainingProgress { Status = TrainingStatus.Completed, Message = "ok" });
        _kafkaProducer
            .Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var firstException = await Record.ExceptionAsync(() => DispatchAsync(consumer, CreateTrainRequestMessage("train_requested", idIa: 30, requestedBy: 1)));
        var secondException = await Record.ExceptionAsync(() => DispatchAsync(consumer, CreateTrainRequestMessage("train_requested", idIa: 31, requestedBy: 1)));

        // Assert
        Assert.Null(firstException);
        Assert.Null(secondException);
    }
}
