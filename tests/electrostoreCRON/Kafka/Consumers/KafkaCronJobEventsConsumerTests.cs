using System.Reflection;
using ElectrostoreCRON.Kafka.Consumers;
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
    private readonly Mock<IServiceProvider> _serviceProvider = new();
    private readonly Mock<ILogger<KafkaCronJobEventsConsumer>> _logger = new();

    public KafkaCronJobEventsConsumerTests()
    {
        _schedulerFactory.Setup(f => f.GetScheduler(It.IsAny<CancellationToken>())).ReturnsAsync(_scheduler.Object);
        _scheduler.Setup(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>())).ReturnsAsync(DateTimeOffset.UtcNow);
    }

    private KafkaCronJobEventsConsumer CreateConsumer()
    {
        var configuration = new ConfigurationBuilder().Build();
        return new KafkaCronJobEventsConsumer(_schedulerFactory.Object, _serviceProvider.Object, configuration, _logger.Object);
    }

    private static Task HandleEventAsync(KafkaCronJobEventsConsumer consumer, string payload, CancellationToken ct = default)
    {
        var method = typeof(KafkaCronJobEventsConsumer).GetMethod("HandleEventAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("HandleEventAsync method not found");
        return (Task)method.Invoke(consumer, new object[] { payload, ct })!;
    }

    [Fact]
    public async Task HandleEventAsync_ShouldDoNothing_WhenPayloadIsInvalidJson()
    {
        // Arrange
        var consumer = CreateConsumer();

        // Act
        var exception = await Record.ExceptionAsync(() => HandleEventAsync(consumer, "{not-json"));

        // Assert
        Assert.Null(exception);
        _schedulerFactory.Verify(f => f.GetScheduler(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleEventAsync_ShouldDoNothing_WhenPayloadDeserializesToNull()
    {
        // Arrange
        var consumer = CreateConsumer();

        // Act
        await HandleEventAsync(consumer, "null");

        // Assert
        _schedulerFactory.Verify(f => f.GetScheduler(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleEventAsync_ShouldScheduleJob_WhenActionIsCreatedAndEnabled()
    {
        // Arrange
        var consumer = CreateConsumer();
        _scheduler.Setup(s => s.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var payload = """
        {"action":"created","data":{"id_cronjob":1,"name_cronjob":"Test","cron_expression":"0 12 1 * ?","action_cronjob":0,"params_cronjob":"","is_enabled":true}}
        """;

        // Act
        await HandleEventAsync(consumer, payload);

        // Assert
        _scheduler.Verify(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once);
        _scheduler.Verify(s => s.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleEventAsync_ShouldNotScheduleJob_WhenActionIsCreatedAndDisabled()
    {
        // Arrange
        var consumer = CreateConsumer();
        var payload = """
        {"action":"created","data":{"id_cronjob":1,"name_cronjob":"Test","cron_expression":"0 12 1 * ?","action_cronjob":0,"params_cronjob":"","is_enabled":false}}
        """;

        // Act
        await HandleEventAsync(consumer, payload);

        // Assert
        _scheduler.Verify(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Never);
        _scheduler.Verify(s => s.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleEventAsync_ShouldNotScheduleJob_WhenCronExpressionIsEmpty()
    {
        // Arrange
        var consumer = CreateConsumer();
        var payload = """
        {"action":"created","data":{"id_cronjob":1,"name_cronjob":"Test","cron_expression":"","action_cronjob":0,"params_cronjob":"","is_enabled":true}}
        """;

        // Act
        await HandleEventAsync(consumer, payload);

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
        var payload = """
        {"action":"updated","data":{"id_cronjob":2,"name_cronjob":"Test","cron_expression":"0 12 1 * ?","action_cronjob":0,"params_cronjob":"","is_enabled":true}}
        """;

        // Act
        await HandleEventAsync(consumer, payload);

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
        var payload = """
        {"action":"updated","data":{"id_cronjob":3,"name_cronjob":"Test","cron_expression":"0 12 1 * ?","action_cronjob":0,"params_cronjob":"","is_enabled":false}}
        """;

        // Act
        await HandleEventAsync(consumer, payload);

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
        var payload = """
        {"action":"deleted","data":{"id_cronjob":4,"name_cronjob":"Test","cron_expression":"0 12 1 * ?","action_cronjob":0,"params_cronjob":"","is_enabled":true}}
        """;

        // Act
        await HandleEventAsync(consumer, payload);

        // Assert
        _scheduler.Verify(s => s.DeleteJob(It.Is<JobKey>(k => k.Name == "job-4"), It.IsAny<CancellationToken>()), Times.Once);
        _scheduler.Verify(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleEventAsync_ShouldDoNothing_WhenActionIsUnknown()
    {
        // Arrange
        var consumer = CreateConsumer();
        var payload = """
        {"action":"unknown-action","data":{"id_cronjob":5,"name_cronjob":"Test","cron_expression":"0 12 1 * ?","action_cronjob":0,"params_cronjob":"","is_enabled":true}}
        """;

        // Act
        var exception = await Record.ExceptionAsync(() => HandleEventAsync(consumer, payload));

        // Assert
        Assert.Null(exception);
        _scheduler.Verify(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Never);
        _scheduler.Verify(s => s.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
