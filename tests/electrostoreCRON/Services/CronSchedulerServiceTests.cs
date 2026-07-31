using System.Reflection;
using ElectrostoreCRON.Grpc;
using ElectrostoreCRON.Services.CronSchedulerService;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using Xunit;

namespace ElectrostoreCRON.Tests.Services;

public class CronSchedulerServiceTests
{
    private readonly Mock<ISchedulerFactory> _schedulerFactory = new();
    private readonly Mock<CronJobsGrpc.CronJobsGrpcClient> _apiClient = new();
    private readonly Mock<ILogger<CronSchedulerService>> _logger = new();

    private CronSchedulerService CreateService()
    {
        var configuration = new ConfigurationBuilder().Build();
        return new CronSchedulerService(_schedulerFactory.Object, _apiClient.Object, configuration, _logger.Object);
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

    private static AsyncUnaryCall<TResponse> CreateFailingAsyncUnaryCall<TResponse>(Exception exception)
    {
        return new AsyncUnaryCall<TResponse>(
            Task.FromException<TResponse>(exception),
            Task.FromException<Metadata>(exception),
            () => Status.DefaultCancelled,
            () => new Metadata(),
            () => { });
    }

    private static Task LoadAndScheduleJobsAsync(CronSchedulerService service, IScheduler scheduler, CancellationToken ct = default)
    {
        var method = typeof(CronSchedulerService).GetMethod("LoadAndScheduleJobsAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("LoadAndScheduleJobsAsync method not found");
        return (Task)method.Invoke(service, new object[] { scheduler, ct })!;
    }

    [Fact]
    public async Task LoadAndScheduleJobsAsync_ShouldScheduleJob_WithNormalizedCronExpression()
    {
        // Arrange
        var service = CreateService();
        var scheduler = new Mock<IScheduler>();
        IJobDetail? scheduledJobDetail = null;
        ITrigger? scheduledTrigger = null;
        scheduler
            .Setup(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()))
            .Callback<IJobDetail, ITrigger, CancellationToken>((jd, t, _) => { scheduledJobDetail = jd; scheduledTrigger = t; })
            .ReturnsAsync(DateTimeOffset.UtcNow);

        var reply = new GetEnabledCronJobsReply();
        reply.CronJobs.Add(new CronJobItem
        {
            IdCronjob = 1,
            NameCronjob = "Test",
            // 5-field cron with an explicit "?" on day-of-week: Quartz requires day-of-month and
            // day-of-week not both be "*", and the production code does not translate a plain "*"
            // for either field, so a realistic schedulable expression must supply "?" itself.
            CronExpression = "0 12 1 * ?",
            ActionCronjob = CronJobAction.PackageTracking,
            ParamsCronjob = ""
        });
        _apiClient
            .Setup(c => c.GetEnabledCronJobsAsync(It.IsAny<GetEnabledCronJobsRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(reply));

        // Act
        await LoadAndScheduleJobsAsync(service, scheduler.Object);

        // Assert
        scheduler.Verify(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(scheduledJobDetail);
        Assert.Equal("job-1", scheduledJobDetail!.Key.Name);
        Assert.Equal("electrostore", scheduledJobDetail.Key.Group);
        var cronTrigger = Assert.IsAssignableFrom<ICronTrigger>(scheduledTrigger);
        Assert.Equal("0 0 12 1 * ?", cronTrigger.CronExpressionString);
    }

    [Fact]
    public async Task LoadAndScheduleJobsAsync_ShouldSkipJob_WhenCronExpressionIsEmpty()
    {
        // Arrange
        var service = CreateService();
        var scheduler = new Mock<IScheduler>();

        var reply = new GetEnabledCronJobsReply();
        reply.CronJobs.Add(new CronJobItem
        {
            IdCronjob = 1,
            NameCronjob = "Empty",
            CronExpression = "",
            ActionCronjob = CronJobAction.PackageTracking,
        });
        _apiClient
            .Setup(c => c.GetEnabledCronJobsAsync(It.IsAny<GetEnabledCronJobsRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(reply));

        // Act
        await LoadAndScheduleJobsAsync(service, scheduler.Object);

        // Assert
        scheduler.Verify(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoadAndScheduleJobsAsync_ShouldSkipInvalidJob_AndContinueSchedulingOthers()
    {
        // Arrange
        var service = CreateService();
        var scheduler = new Mock<IScheduler>();
        scheduler
            .Setup(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DateTimeOffset.UtcNow);

        var reply = new GetEnabledCronJobsReply();
        reply.CronJobs.Add(new CronJobItem
        {
            IdCronjob = 1,
            NameCronjob = "Invalid",
            CronExpression = "not-a-cron-expression",
            ActionCronjob = CronJobAction.PackageTracking,
        });
        reply.CronJobs.Add(new CronJobItem
        {
            IdCronjob = 2,
            NameCronjob = "Valid",
            CronExpression = "0 12 1 * ?",
            ActionCronjob = CronJobAction.PackageTracking,
        });
        _apiClient
            .Setup(c => c.GetEnabledCronJobsAsync(It.IsAny<GetEnabledCronJobsRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(reply));

        // Act
        await LoadAndScheduleJobsAsync(service, scheduler.Object);

        // Assert
        scheduler.Verify(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadAndScheduleJobsAsync_ShouldNotScheduleAnything_WhenApiCallFails()
    {
        // Arrange
        var service = CreateService();
        var scheduler = new Mock<IScheduler>();
        _apiClient
            .Setup(c => c.GetEnabledCronJobsAsync(It.IsAny<GetEnabledCronJobsRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateFailingAsyncUnaryCall<GetEnabledCronJobsReply>(new RpcException(new Status(StatusCode.Unavailable, "down"))));

        // Act
        var exception = await Record.ExceptionAsync(() => LoadAndScheduleJobsAsync(service, scheduler.Object));

        // Assert
        Assert.Null(exception);
        scheduler.Verify(s => s.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
