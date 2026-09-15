using ElectrostoreCRON.Grpc;
using ElectrostoreCRON.Services.CronSchedulerService;

using ElectrostoreCRON.Services.ItemMovementReportService;
using ElectrostoreCRON.Services.StockLowAlertService;
using ElectrostoreCRON.Services.CronJobExecutionRegistry;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using Xunit;

namespace ElectrostoreCRON.Tests.Services;

public class ElectrostoreCronJobTests
{
    private readonly Mock<IItemMovementReportService> _itemMovementReport = new();
    private readonly Mock<IStockLowAlertService> _stockLowAlert = new();
    private readonly Mock<ICronJobExecutionRegistry> _executionRegistry = new();
    private readonly Mock<CronJobsGrpc.CronJobsGrpcClient> _apiClient = new();
    private readonly Mock<ILogger<ElectrostoreCronJob>> _logger = new();

    public ElectrostoreCronJobTests()
    {
        _executionRegistry
            .Setup(r => r.Register(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(CancellationToken.None);
        _apiClient
            .Setup(c => c.UpdateCronJobStatusAsync(It.IsAny<UpdateCronJobStatusRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(new UpdateCronJobStatusReply { Success = true }));
        _apiClient
            .Setup(c => c.UpdateCronJobRunAsync(It.IsAny<UpdateCronJobRunRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(new UpdateCronJobRunReply { Success = true }));
    }

    private ElectrostoreCronJob CreateJob() => new(_itemMovementReport.Object, _stockLowAlert.Object, _executionRegistry.Object, _apiClient.Object, _logger.Object);

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

    private static Mock<IJobExecutionContext> CreateContext(int id, CronJobAction? action, DateTimeOffset? nextFireTimeUtc = null, string jobParams = "", string lastRunAt = "")
    {
        var dataMap = new JobDataMap();
        dataMap.Put(ElectrostoreCronJob.KeyId, id);
        if (action.HasValue)
        {
            dataMap.Put(ElectrostoreCronJob.KeyAction, (int)action.Value);
        }
        dataMap.Put(ElectrostoreCronJob.KeyParams, jobParams);
        dataMap.Put(ElectrostoreCronJob.KeyLastRunAt, lastRunAt);

        var jobDetail = new Mock<IJobDetail>();
        jobDetail.SetupGet(d => d.JobDataMap).Returns(dataMap);

        var context = new Mock<IJobExecutionContext>();
        context.SetupGet(c => c.JobDetail).Returns(jobDetail.Object);
        context.SetupGet(c => c.NextFireTimeUtc).Returns(nextFireTimeUtc);
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return context;
    }

    [Fact]
    public async Task Execute_WeeklyItemMovementReportAction_CallsReportService_AndSetsSuccessStatus()
    {
        // Arrange
        var job = CreateJob();
        var context = CreateContext(1, CronJobAction.WeeklyItemMovementReport, jobParams: "{}", lastRunAt: "2026-01-01T00:00:00.0000000Z");

        // Act
        await job.Execute(context.Object);

        // Assert
        _itemMovementReport.Verify(
            s => s.SendReportAsync("{}", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), It.IsAny<CancellationToken>()),
            Times.Once);
        _stockLowAlert.Verify(s => s.SendAlertAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
        _apiClient.Verify(
            c => c.UpdateCronJobStatusAsync(
                It.Is<UpdateCronJobStatusRequest>(r => r.IdCronjob == 1 && r.StatusCronjob == CronJobExecutionStatus.Success),
                It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_StockLowAlertAction_CallsAlertService_AndSetsSuccessStatus()
    {
        // Arrange
        var job = CreateJob();
        var context = CreateContext(2, CronJobAction.StockLowAlert, jobParams: "{}");

        // Act
        await job.Execute(context.Object);

        // Assert
        _stockLowAlert.Verify(s => s.SendAlertAsync("{}", null, It.IsAny<CancellationToken>()), Times.Once);
        _itemMovementReport.Verify(s => s.SendReportAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
        _apiClient.Verify(
            c => c.UpdateCronJobStatusAsync(
                It.Is<UpdateCronJobStatusRequest>(r => r.IdCronjob == 2 && r.StatusCronjob == CronJobExecutionStatus.Success),
                It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_PackageTrackingAction_CallsNoService_AndSetsSuccessStatus()
    {
        // Arrange
        var job = CreateJob();
        var context = CreateContext(3, CronJobAction.PackageTracking);

        // Act
        await job.Execute(context.Object);

        // Assert
        _itemMovementReport.Verify(s => s.SendReportAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
        _stockLowAlert.Verify(s => s.SendAlertAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
        _apiClient.Verify(
            c => c.UpdateCronJobStatusAsync(
                It.Is<UpdateCronJobStatusRequest>(r => r.IdCronjob == 3 && r.StatusCronjob == CronJobExecutionStatus.Success),
                It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_UnknownAction_SkipsExecution_AndSetsSuccessStatus()
    {
        // Arrange
        var job = CreateJob();
        var context = CreateContext(4, action: null);

        // Act
        await job.Execute(context.Object);

        // Assert
        _itemMovementReport.Verify(s => s.SendReportAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
        _stockLowAlert.Verify(s => s.SendAlertAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
        _apiClient.Verify(
            c => c.UpdateCronJobStatusAsync(
                It.Is<UpdateCronJobStatusRequest>(r => r.IdCronjob == 4 && r.StatusCronjob == CronJobExecutionStatus.Success),
                It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_InvalidLastRunAt_IsPassedAsNull()
    {
        // Arrange
        var job = CreateJob();
        var context = CreateContext(5, CronJobAction.StockLowAlert, lastRunAt: "not-a-date");

        // Act
        await job.Execute(context.Object);

        // Assert
        _stockLowAlert.Verify(s => s.SendAlertAsync(It.IsAny<string?>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_SetsRunningStatus_BeforeExecutingAction()
    {
        // Arrange
        var job = CreateJob();
        var context = CreateContext(6, CronJobAction.PackageTracking);

        // Act
        await job.Execute(context.Object);

        // Assert
        _apiClient.Verify(
            c => c.UpdateCronJobStatusAsync(
                It.Is<UpdateCronJobStatusRequest>(r => r.IdCronjob == 6 && r.StatusCronjob == CronJobExecutionStatus.Running),
                It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_ForceStopped_WhenOperationCanceledMatchesRunToken_SetsStoppedStatus()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        _executionRegistry
            .Setup(r => r.Register(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(cts.Token);
        _itemMovementReport
            .Setup(s => s.SendReportAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var job = CreateJob();
        var context = CreateContext(7, CronJobAction.WeeklyItemMovementReport);

        // Act
        var exception = await Record.ExceptionAsync(() => job.Execute(context.Object));

        // Assert
        Assert.Null(exception);
        _apiClient.Verify(
            c => c.UpdateCronJobStatusAsync(
                It.Is<UpdateCronJobStatusRequest>(r => r.IdCronjob == 7 && r.StatusCronjob == CronJobExecutionStatus.Stopped),
                It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_OperationCanceled_WhenRunTokenNotCancelled_SetsFailedStatus()
    {
        // Arrange
        // No RequestStop was issued, so runToken is not cancelled: the "when" clause on the
        // OperationCanceledException handler must not match, falling through to the generic handler.
        _itemMovementReport
            .Setup(s => s.SendReportAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("unrelated cancellation"));

        var job = CreateJob();
        var context = CreateContext(8, CronJobAction.WeeklyItemMovementReport);

        // Act
        await job.Execute(context.Object);

        // Assert
        _apiClient.Verify(
            c => c.UpdateCronJobStatusAsync(
                It.Is<UpdateCronJobStatusRequest>(r => r.IdCronjob == 8 && r.StatusCronjob == CronJobExecutionStatus.Failed),
                It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_ServiceThrows_SetsFailedStatus_WithExceptionMessage()
    {
        // Arrange
        _stockLowAlert
            .Setup(s => s.SendAlertAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var job = CreateJob();
        var context = CreateContext(9, CronJobAction.StockLowAlert);

        // Act
        await job.Execute(context.Object);

        // Assert
        _apiClient.Verify(
            c => c.UpdateCronJobStatusAsync(
                It.Is<UpdateCronJobStatusRequest>(r => r.IdCronjob == 9 && r.StatusCronjob == CronJobExecutionStatus.Failed && r.LastErrorCronjob == "boom"),
                It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_Always_UnregistersJob_FromExecutionRegistry()
    {
        // Arrange
        var job = CreateJob();
        var context = CreateContext(10, CronJobAction.PackageTracking);

        // Act
        await job.Execute(context.Object);

        // Assert
        _executionRegistry.Verify(r => r.Unregister(10), Times.Once);
    }

    [Fact]
    public async Task Execute_Always_UnregistersJob_EvenWhenActionThrows()
    {
        // Arrange
        _stockLowAlert
            .Setup(s => s.SendAlertAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var job = CreateJob();
        var context = CreateContext(11, CronJobAction.StockLowAlert);

        // Act
        await job.Execute(context.Object);

        // Assert
        _executionRegistry.Verify(r => r.Unregister(11), Times.Once);
    }

    [Fact]
    public async Task Execute_UpdatesLastRunAt_WithNextFireTime_WhenScheduled()
    {
        // Arrange
        var job = CreateJob();
        var nextFireTime = new DateTimeOffset(2026, 2, 1, 3, 0, 0, TimeSpan.Zero);
        var context = CreateContext(12, CronJobAction.PackageTracking, nextFireTimeUtc: nextFireTime);
        UpdateCronJobRunRequest? capturedRequest = null;
        _apiClient
            .Setup(c => c.UpdateCronJobRunAsync(It.IsAny<UpdateCronJobRunRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Callback<UpdateCronJobRunRequest, Metadata, DateTime?, CancellationToken>((r, _, _, _) => capturedRequest = r)
            .Returns(CreateAsyncUnaryCall(new UpdateCronJobRunReply { Success = true }));

        // Act
        await job.Execute(context.Object);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(12, capturedRequest!.IdCronjob);
        Assert.Equal(nextFireTime.UtcDateTime.ToString("O"), capturedRequest.NextRunAt);
        Assert.False(string.IsNullOrEmpty(capturedRequest.LastRunAt));
    }

    [Fact]
    public async Task Execute_UpdatesLastRunAt_WithEmptyNextRunAt_WhenNotScheduled()
    {
        // Arrange
        var job = CreateJob();
        var context = CreateContext(13, CronJobAction.PackageTracking, nextFireTimeUtc: null);
        UpdateCronJobRunRequest? capturedRequest = null;
        _apiClient
            .Setup(c => c.UpdateCronJobRunAsync(It.IsAny<UpdateCronJobRunRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Callback<UpdateCronJobRunRequest, Metadata, DateTime?, CancellationToken>((r, _, _, _) => capturedRequest = r)
            .Returns(CreateAsyncUnaryCall(new UpdateCronJobRunReply { Success = true }));

        // Act
        await job.Execute(context.Object);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(string.Empty, capturedRequest!.NextRunAt);
    }

    [Fact]
    public async Task Execute_DoesNotThrow_WhenUpdateCronJobRunAsyncFails()
    {
        // Arrange
        var job = CreateJob();
        var context = CreateContext(14, CronJobAction.PackageTracking);
        _apiClient
            .Setup(c => c.UpdateCronJobRunAsync(It.IsAny<UpdateCronJobRunRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateFailingAsyncUnaryCall<UpdateCronJobRunReply>(new RpcException(new Status(StatusCode.Unavailable, "down"))));

        // Act
        var exception = await Record.ExceptionAsync(() => job.Execute(context.Object));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task Execute_DoesNotThrow_WhenUpdateCronJobStatusAsyncFails()
    {
        // Arrange
        var job = CreateJob();
        var context = CreateContext(15, CronJobAction.PackageTracking);
        _apiClient
            .Setup(c => c.UpdateCronJobStatusAsync(It.IsAny<UpdateCronJobStatusRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateFailingAsyncUnaryCall<UpdateCronJobStatusReply>(new RpcException(new Status(StatusCode.Unavailable, "down"))));

        // Act
        var exception = await Record.ExceptionAsync(() => job.Execute(context.Object));

        // Assert
        Assert.Null(exception);
    }
}
