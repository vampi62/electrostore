using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Grpc;
using ElectrostoreAPI.Grpc.Services;
using ElectrostoreAPI.Services.CronJobService;
using ElectrostoreAPI.Tests.Utils;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElectrostoreAPI.Tests.Grpc;

public class CronJobsGrpcServiceTests
{
    private readonly Mock<ICronJobService> _cronJobService = new();
    private readonly Mock<ILogger<CronJobsGrpcService>> _logger = new();

    private CronJobsGrpcService CreateService() => new(_cronJobService.Object, _logger.Object);

    // --- GetEnabledCronJobs ---

    [Fact]
    public async Task GetEnabledCronJobs_ShouldMapAllFields()
    {
        var lastRun = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var nextRun = new DateTime(2026, 1, 3, 3, 4, 5, DateTimeKind.Utc);
        _cronJobService
            .Setup(s => s.GetEnabledCronJobsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReadCronJobDto>
            {
                new()
                {
                    id_cronjob = 7,
                    name_cronjob = "stock",
                    cron_expression_cronjob = "0 8 * * *",
                    action_cronjob = Enums.CronJobAction.StockLowAlert,
                    params_cronjob = "{\"a\":1}",
                    last_run_at = lastRun,
                    next_run_at = nextRun
                }
            });
        var service = CreateService();

        var reply = await service.GetEnabledCronJobs(new GetEnabledCronJobsRequest(), TestServerCallContext.Create());

        var job = Assert.Single(reply.CronJobs);
        Assert.Equal(7, job.IdCronjob);
        Assert.Equal("stock", job.NameCronjob);
        Assert.Equal("0 8 * * *", job.CronExpressionCronjob);
        Assert.Equal((int)Enums.CronJobAction.StockLowAlert, (int)job.ActionCronjob);
        Assert.Equal("{\"a\":1}", job.ParamsCronjob);
        Assert.Equal(lastRun.ToString("o"), job.LastRunAt);
        Assert.Equal(nextRun.ToString("o"), job.NextRunAt);
    }

    [Fact]
    public async Task GetEnabledCronJobs_ShouldReturnEmptyStrings_WhenOptionalFieldsAreNull()
    {
        _cronJobService
            .Setup(s => s.GetEnabledCronJobsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReadCronJobDto>
            {
                new() { id_cronjob = 1, name_cronjob = "n", cron_expression_cronjob = "* * * * *" }
            });
        var service = CreateService();

        var reply = await service.GetEnabledCronJobs(new GetEnabledCronJobsRequest(), TestServerCallContext.Create());

        var job = Assert.Single(reply.CronJobs);
        Assert.Equal(string.Empty, job.ParamsCronjob);
        Assert.Equal(string.Empty, job.LastRunAt);
        Assert.Equal(string.Empty, job.NextRunAt);
    }

    [Fact]
    public async Task GetEnabledCronJobs_ShouldReturnEmptyReply_WhenNoJob()
    {
        _cronJobService
            .Setup(s => s.GetEnabledCronJobsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReadCronJobDto>());
        var service = CreateService();

        var reply = await service.GetEnabledCronJobs(new GetEnabledCronJobsRequest(), TestServerCallContext.Create());

        Assert.Empty(reply.CronJobs);
    }

    // --- UpdateCronJobRun ---

    [Fact]
    public async Task UpdateCronJobRun_ShouldParseDatesAndReturnSuccess()
    {
        var lastRun = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc);
        var nextRun = new DateTime(2026, 5, 2, 10, 0, 0, DateTimeKind.Utc);
        var service = CreateService();

        var reply = await service.UpdateCronJobRun(new UpdateCronJobRunRequest
        {
            IdCronjob = 3,
            LastRunAt = lastRun.ToString("o"),
            NextRunAt = nextRun.ToString("o")
        }, TestServerCallContext.Create());

        Assert.True(reply.Success);
        _cronJobService.Verify(s => s.UpdateCronJobRunAsync(3, lastRun, nextRun, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCronJobRun_ShouldPassNullDates_WhenEmptyOrWhitespace()
    {
        var service = CreateService();

        var reply = await service.UpdateCronJobRun(new UpdateCronJobRunRequest
        {
            IdCronjob = 3,
            LastRunAt = "",
            NextRunAt = "   "
        }, TestServerCallContext.Create());

        Assert.True(reply.Success);
        _cronJobService.Verify(s => s.UpdateCronJobRunAsync(3, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCronJobRun_ShouldReturnFailure_WhenServiceThrows()
    {
        _cronJobService
            .Setup(s => s.UpdateCronJobRunAsync(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());
        var service = CreateService();

        var reply = await service.UpdateCronJobRun(new UpdateCronJobRunRequest { IdCronjob = 99 }, TestServerCallContext.Create());

        Assert.False(reply.Success);
    }

    [Fact]
    public async Task UpdateCronJobRun_ShouldReturnFailure_WhenDateIsInvalid()
    {
        var service = CreateService();

        var reply = await service.UpdateCronJobRun(new UpdateCronJobRunRequest { IdCronjob = 1, LastRunAt = "not-a-date" }, TestServerCallContext.Create());

        Assert.False(reply.Success);
        _cronJobService.Verify(s => s.UpdateCronJobRunAsync(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // --- UpdateCronJobStatus ---

    [Fact]
    public async Task UpdateCronJobStatus_ShouldForwardStatusAndError()
    {
        var service = CreateService();

        var reply = await service.UpdateCronJobStatus(new UpdateCronJobStatusRequest
        {
            IdCronjob = 5,
            StatusCronjob = CronJobExecutionStatus.Failed,
            LastErrorCronjob = "boom"
        }, TestServerCallContext.Create());

        Assert.True(reply.Success);
        _cronJobService.Verify(s => s.UpdateCronJobStatusAsync(5, CronJobStatus.Failed, "boom", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCronJobStatus_ShouldPassNullError_WhenEmpty()
    {
        var service = CreateService();

        var reply = await service.UpdateCronJobStatus(new UpdateCronJobStatusRequest
        {
            IdCronjob = 5,
            StatusCronjob = CronJobExecutionStatus.Running,
            LastErrorCronjob = " "
        }, TestServerCallContext.Create());

        Assert.True(reply.Success);
        _cronJobService.Verify(s => s.UpdateCronJobStatusAsync(5, CronJobStatus.Running, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCronJobStatus_ShouldReturnFailure_WhenServiceThrows()
    {
        _cronJobService
            .Setup(s => s.UpdateCronJobStatusAsync(It.IsAny<int>(), It.IsAny<CronJobStatus>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());
        var service = CreateService();

        var reply = await service.UpdateCronJobStatus(new UpdateCronJobStatusRequest { IdCronjob = 5 }, TestServerCallContext.Create());

        Assert.False(reply.Success);
    }
}
