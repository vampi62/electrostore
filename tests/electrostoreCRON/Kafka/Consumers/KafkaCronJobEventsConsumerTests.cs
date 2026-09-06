using System.Reflection;
using Confluent.Kafka;
using ElectrostoreCRON.Kafka.Consumers;
using ElectrostoreCRON.Kafka.Messages;
using ElectrostoreCRON.Services.CronJobExecutionRegistry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using Xunit;

namespace ElectrostoreCRON.Tests.Kafka.Consumers;

public class KafkaCronJobEventsConsumerTests
{
    private readonly Mock<ISchedulerFactory> _schedulerFactory = new();
    private readonly Mock<IScheduler> _scheduler = new();
    private readonly Mock<ICronJobExecutionRegistry> _cronJobExecutionRegistry = new();
    private readonly Mock<ILogger<KafkaCronJobEventsConsumer>> _logger = new();

    public KafkaCronJobEventsConsumerTests()
    {
        _schedulerFactory.Setup(f => f.GetScheduler(It.IsAny<CancellationToken>())).ReturnsAsync(_scheduler.Object);
        _scheduler.Setup(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>())).ReturnsAsync(DateTimeOffset.UtcNow);
    }

    private KafkaCronJobEventsConsumer CreateConsumer()
    {
        var configuration = new ConfigurationBuilder().Build();
        return new KafkaCronJobEventsConsumer(_schedulerFactory.Object, _cronJobExecutionRegistry.Object, configuration, _logger.Object);
    }

    private static Task<bool> HandleEventAsync(KafkaCronJobEventsConsumer consumer, CronJobEvent? evt, CancellationToken ct = default)
    {
        var method = typeof(KafkaCronJobEventsConsumer).GetMethod("HandleEventAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("HandleEventAsync method not found");
        return (Task<bool>)method.Invoke(consumer, new object?[] { evt, ct })!;
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

    private static Task<ConsumeResult<string, string>?> ConsumeMessageAsync(KafkaCronJobEventsConsumer consumer, IConsumer<string, string> kafkaConsumer, CancellationToken ct = default)
    {
        var method = typeof(KafkaCronJobEventsConsumer).GetMethod("ConsumeMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ConsumeMessageAsync method not found");
        return (Task<ConsumeResult<string, string>?>)method.Invoke(consumer, new object[] { kafkaConsumer, ct })!;
    }

    private static Task ProcessMessageAsync(KafkaCronJobEventsConsumer consumer, IConsumer<string, string> kafkaConsumer, ConsumeResult<string, string> result, CancellationToken ct = default)
    {
        var method = typeof(KafkaCronJobEventsConsumer).GetMethod("ProcessMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ProcessMessageAsync method not found");
        return (Task)method.Invoke(consumer, new object[] { kafkaConsumer, result, ct })!;
    }

    private static CronJobEvent? DeserializeMessage(KafkaCronJobEventsConsumer consumer, ConsumeResult<string, string> result)
    {
        var method = typeof(KafkaCronJobEventsConsumer).GetMethod("DeserializeMessage", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("DeserializeMessage method not found");
        return (CronJobEvent?)method.Invoke(consumer, new object[] { result });
    }

    [Fact]
    public async Task HandleEventAsync_ShouldReturnFalse_WhenEventIsNull()
    {
        // Arrange
        var consumer = CreateConsumer();

        // Act
        var dispatched = await HandleEventAsync(consumer, null);

        // Assert
        Assert.False(dispatched);
        _schedulerFactory.Verify(f => f.GetScheduler(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleEventAsync_ShouldScheduleJob_WhenActionIsCreatedAndEnabled()
    {
        // Arrange
        var consumer = CreateConsumer();
        _scheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var evt = new CronJobEvent
        {
            action = "created",
            data = new CronJobEventData { id_cronjob = 1, name_cronjob = "Test", cron_expression_cronjob = "0 12 1 * ?", action_cronjob = 0, params_cronjob = "", is_enabled = true }
        };

        // Act
        await HandleEventAsync(consumer, evt);

        // Assert
        _scheduler.Verify(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once);
        _scheduler.Verify(s => s.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleEventAsync_ShouldNotScheduleJob_WhenActionIsCreatedAndDisabled()
    {
        // Arrange
        var consumer = CreateConsumer();
        var evt = new CronJobEvent
        {
            action = "created",
            data = new CronJobEventData { id_cronjob = 1, name_cronjob = "Test", cron_expression_cronjob = "0 12 1 * ?", action_cronjob = 0, params_cronjob = "", is_enabled = false }
        };

        // Act
        await HandleEventAsync(consumer, evt);

        // Assert
        _scheduler.Verify(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Never);
        _scheduler.Verify(s => s.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleEventAsync_ShouldNotScheduleJob_WhenCronExpressionIsEmpty()
    {
        // Arrange
        var consumer = CreateConsumer();
        var evt = new CronJobEvent
        {
            action = "created",
            data = new CronJobEventData { id_cronjob = 1, name_cronjob = "Test", cron_expression_cronjob = "", action_cronjob = 0, params_cronjob = "", is_enabled = true }
        };

        // Act
        await HandleEventAsync(consumer, evt);

        // Assert
        _scheduler.Verify(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Never);
        _scheduler.Verify(s => s.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleEventAsync_ShouldRemoveAndRescheduleJob_WhenActionIsUpdatedAndEnabled()
    {
        // Arrange
        var consumer = CreateConsumer();
        _scheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _scheduler.Setup(s => s.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var evt = new CronJobEvent
        {
            action = "updated",
            data = new CronJobEventData { id_cronjob = 2, name_cronjob = "Test", cron_expression_cronjob = "0 12 1 * ?", action_cronjob = 0, params_cronjob = "", is_enabled = true }
        };

        // Act
        await HandleEventAsync(consumer, evt);

        // Assert
        // "updated" removes the existing job before delegating to ScheduleOrReplaceJobAsync,
        // which itself also removes any existing job before scheduling - hence two deletions.
        _scheduler.Verify(s => s.DeleteJob(It.Is<JobKey>(k => k.Name == "job-2"), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _scheduler.Verify(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleEventAsync_ShouldOnlyRemoveJob_WhenActionIsUpdatedAndDisabled()
    {
        // Arrange
        var consumer = CreateConsumer();
        _scheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _scheduler.Setup(s => s.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var evt = new CronJobEvent
        {
            action = "updated",
            data = new CronJobEventData { id_cronjob = 3, name_cronjob = "Test", cron_expression_cronjob = "0 12 1 * ?", action_cronjob = 0, params_cronjob = "", is_enabled = false }
        };

        // Act
        await HandleEventAsync(consumer, evt);

        // Assert
        _scheduler.Verify(s => s.DeleteJob(It.Is<JobKey>(k => k.Name == "job-3"), It.IsAny<CancellationToken>()), Times.Once);
        _scheduler.Verify(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleEventAsync_ShouldRemoveJob_WhenActionIsDeleted()
    {
        // Arrange
        var consumer = CreateConsumer();
        _scheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _scheduler.Setup(s => s.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var evt = new CronJobEvent
        {
            action = "deleted",
            data = new CronJobEventData { id_cronjob = 4, name_cronjob = "Test", cron_expression_cronjob = "0 12 1 * ?", action_cronjob = 0, params_cronjob = "", is_enabled = true }
        };

        // Act
        await HandleEventAsync(consumer, evt);

        // Assert
        _scheduler.Verify(s => s.DeleteJob(It.Is<JobKey>(k => k.Name == "job-4"), It.IsAny<CancellationToken>()), Times.Once);
        _scheduler.Verify(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleEventAsync_ShouldDoNothing_WhenActionIsUnknown()
    {
        // Arrange
        var consumer = CreateConsumer();
        var evt = new CronJobEvent
        {
            action = "unknown-action",
            data = new CronJobEventData { id_cronjob = 5, name_cronjob = "Test", cron_expression_cronjob = "0 12 1 * ?", action_cronjob = 0, params_cronjob = "", is_enabled = true }
        };

        // Act
        var exception = await Record.ExceptionAsync(() => HandleEventAsync(consumer, evt));

        // Assert
        Assert.Null(exception);
        _scheduler.Verify(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Never);
        _scheduler.Verify(s => s.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- DeserializeMessage ----

    [Fact]
    public void DeserializeMessage_ShouldReturnMessage_WhenJsonIsValid()
    {
        // Arrange
        var consumer = CreateConsumer();
        var result = CreateConsumeResult("""{"action":"deleted","data":{"id_cronjob":9}}""");

        // Act
        var msg = DeserializeMessage(consumer, result);

        // Assert
        Assert.NotNull(msg);
        Assert.Equal("deleted", msg!.action);
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
        var expected = CreateConsumeResult("""{"action":"deleted","data":{"id_cronjob":9}}""");
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
        _schedulerFactory.Verify(f => f.GetScheduler(It.IsAny<CancellationToken>()), Times.Never);
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
        _schedulerFactory.Verify(f => f.GetScheduler(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldDispatchAndCommit_WhenMessageIsValid()
    {
        // Arrange
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        var result = CreateConsumeResult("""{"action":"deleted","data":{"id_cronjob":9}}""");
        _scheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _scheduler.Setup(s => s.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        await ProcessMessageAsync(consumer, kafkaConsumer.Object, result);

        // Assert
        _scheduler.Verify(s => s.DeleteJob(It.Is<JobKey>(k => k.Name == "job-9"), It.IsAny<CancellationToken>()), Times.Once);
        kafkaConsumer.Verify(c => c.Commit(result), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldNotCommitOrThrow_WhenDispatchThrows()
    {
        // Arrange
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        var result = CreateConsumeResult("""{"action":"deleted","data":{"id_cronjob":9}}""");
        _scheduler
            .Setup(s => s.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("scheduler down"));

        // Act
        var exception = await Record.ExceptionAsync(() => ProcessMessageAsync(consumer, kafkaConsumer.Object, result));

        // Assert
        Assert.Null(exception);
        kafkaConsumer.Verify(c => c.Commit(It.IsAny<ConsumeResult<string, string>>()), Times.Never);
    }
}
