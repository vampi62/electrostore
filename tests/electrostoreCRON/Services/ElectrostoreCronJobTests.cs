using ElectrostoreCRON.Grpc;
using ElectrostoreCRON.Services.CronSchedulerService;
using ElectrostoreCRON.Services.Track17SyncService;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using Xunit;

namespace ElectrostoreCRON.Tests.Services;

public class ElectrostoreCronJobTests
{
    private readonly Mock<ITrack17SyncService> _track17Sync = new();
    private readonly Mock<CronJobsGrpc.CronJobsGrpcClient> _apiClient = new();
    private readonly Mock<ILogger<ElectrostoreCronJob>> _logger = new();

    private ElectrostoreCronJob CreateJob() => new(_track17Sync.Object, _apiClient.Object, _logger.Object);

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

    private static Mock<IJobExecutionContext> CreateContext(int id, CronJobAction? action, DateTimeOffset? nextFireTimeUtc = null)
    {
        var dataMap = new JobDataMap();
        dataMap.Put(ElectrostoreCronJob.KeyId, id);
        if (action.HasValue)
        {
            dataMap.Put(ElectrostoreCronJob.KeyAction, (int)action.Value);
        }
        dataMap.Put(ElectrostoreCronJob.KeyParams, string.Empty);

        var jobDetail = new Mock<IJobDetail>();
        jobDetail.SetupGet(d => d.JobDataMap).Returns(dataMap);

        var context = new Mock<IJobExecutionContext>();
        context.SetupGet(c => c.JobDetail).Returns(jobDetail.Object);
        context.SetupGet(c => c.NextFireTimeUtc).Returns(nextFireTimeUtc);
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return context;
    }

    [Fact]
    public async Task Execute_ShouldCallTrack17Sync_WhenActionIsPackageTracking()
    {
        // Arrange
        var job = CreateJob();
        var context = CreateContext(5, CronJobAction.PackageTracking);
        _track17Sync.Setup(s => s.SyncAllAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _apiClient
            .Setup(c => c.UpdateCronJobRunAsync(It.IsAny<UpdateCronJobRunRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(new UpdateCronJobRunReply { Success = true }));

        // Act
        await job.Execute(context.Object);

        // Assert
        _track17Sync.Verify(s => s.SyncAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _apiClient.Verify(c => c.UpdateCronJobRunAsync(
            It.Is<UpdateCronJobRunRequest>(r => r.IdCronjob == 5 && !string.IsNullOrEmpty(r.LastRunAt)),
            It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldNotCallTrack17Sync_AndShouldStillUpdateLastRun_WhenActionIsUnknown()
    {
        // Arrange
        var job = CreateJob();
        // IaRetrain and StockLowAlert are not handled by the switch (only PackageTracking is), so this
        // exercises the "unknown action" default branch.
        var context = CreateContext(7, CronJobAction.IaRetrain);
        _apiClient
            .Setup(c => c.UpdateCronJobRunAsync(It.IsAny<UpdateCronJobRunRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(new UpdateCronJobRunReply { Success = true }));

        // Act
        await job.Execute(context.Object);

        // Assert
        _track17Sync.Verify(s => s.SyncAllAsync(It.IsAny<CancellationToken>()), Times.Never);
        _apiClient.Verify(c => c.UpdateCronJobRunAsync(
            It.Is<UpdateCronJobRunRequest>(r => r.IdCronjob == 7),
            It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldStillUpdateLastRun_WhenTrack17SyncThrows()
    {
        // Arrange
        var job = CreateJob();
        var context = CreateContext(1, CronJobAction.PackageTracking);
        _track17Sync.Setup(s => s.SyncAllAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        _apiClient
            .Setup(c => c.UpdateCronJobRunAsync(It.IsAny<UpdateCronJobRunRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(new UpdateCronJobRunReply { Success = true }));

        // Act
        var exception = await Record.ExceptionAsync(() => job.Execute(context.Object));

        // Assert
        Assert.Null(exception);
        _apiClient.Verify(c => c.UpdateCronJobRunAsync(It.IsAny<UpdateCronJobRunRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldNotThrow_WhenUpdateCronJobRunApiFails()
    {
        // Arrange
        var job = CreateJob();
        var context = CreateContext(1, CronJobAction.PackageTracking);
        _track17Sync.Setup(s => s.SyncAllAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _apiClient
            .Setup(c => c.UpdateCronJobRunAsync(It.IsAny<UpdateCronJobRunRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateFailingAsyncUnaryCall<UpdateCronJobRunReply>(new RpcException(new Status(StatusCode.Unavailable, "down"))));

        // Act
        var exception = await Record.ExceptionAsync(() => job.Execute(context.Object));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task Execute_ShouldSendEmptyNextRunAt_WhenNextFireTimeIsNull()
    {
        // Arrange
        var job = CreateJob();
        var context = CreateContext(1, CronJobAction.PackageTracking, nextFireTimeUtc: null);
        _track17Sync.Setup(s => s.SyncAllAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _apiClient
            .Setup(c => c.UpdateCronJobRunAsync(It.IsAny<UpdateCronJobRunRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(new UpdateCronJobRunReply { Success = true }));

        // Act
        await job.Execute(context.Object);

        // Assert
        _apiClient.Verify(c => c.UpdateCronJobRunAsync(
            It.Is<UpdateCronJobRunRequest>(r => r.NextRunAt == string.Empty),
            It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldFormatNextRunAt_WhenNextFireTimeIsProvided()
    {
        // Arrange
        var job = CreateJob();
        var nextFireTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var context = CreateContext(1, CronJobAction.PackageTracking, nextFireTimeUtc: nextFireTime);
        _track17Sync.Setup(s => s.SyncAllAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _apiClient
            .Setup(c => c.UpdateCronJobRunAsync(It.IsAny<UpdateCronJobRunRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(new UpdateCronJobRunReply { Success = true }));

        // Act
        await job.Execute(context.Object);

        // Assert
        _apiClient.Verify(c => c.UpdateCronJobRunAsync(
            It.Is<UpdateCronJobRunRequest>(r => r.NextRunAt == nextFireTime.UtcDateTime.ToString("O")),
            It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
