using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Grpc;
using ElectrostoreAPI.Grpc.Services;
using ElectrostoreAPI.Services.CronJobService;
using ElectrostoreAPI.Tests.Utils;
using DomainCronJobAction = ElectrostoreAPI.Enums.CronJobAction;

namespace ElectrostoreAPI.Tests.Grpc.Services;

public class CronJobsGrpcServiceTests
{
    private readonly Mock<ICronJobService> _cronJobService;
    private readonly CronJobsGrpcService _service;

    public CronJobsGrpcServiceTests()
    {
        _cronJobService = new Mock<ICronJobService>();
        _service = new CronJobsGrpcService(_cronJobService.Object, new LoggerFactory().CreateLogger<CronJobsGrpcService>());
    }

    private static ReadCronJobDto BuildCronJob(int id, DomainCronJobAction action = DomainCronJobAction.PackageTracking, DateTime? lastRun = null, DateTime? nextRun = null, string? paramsCronjob = null)
    {
        return new ReadCronJobDto
        {
            id_cronjob = id,
            name_cronjob = $"Job {id}",
            cron_expression = "* * * * *",
            action_cronjob = action,
            params_cronjob = paramsCronjob,
            is_enabled = true,
            last_run_at = lastRun,
            next_run_at = nextRun,
            created_at = DateTime.UtcNow,
            updated_at = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task GetEnabledCronJobs_ShouldReturnEmpty_WhenNoJobsEnabled()
    {
        // Arrange
        _cronJobService.Setup(s => s.GetEnabledCronJobsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReadCronJobDto>());

        // Act
        var reply = await _service.GetEnabledCronJobs(new GetEnabledCronJobsRequest(), TestServerCallContext.Create());

        // Assert
        Assert.Empty(reply.CronJobs);
    }

    [Fact]
    public async Task GetEnabledCronJobs_ShouldMapAllFields_IncludingActionEnum()
    {
        // Arrange
        var lastRun = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var nextRun = new DateTime(2026, 1, 2, 12, 0, 0, DateTimeKind.Utc);
        _cronJobService.Setup(s => s.GetEnabledCronJobsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReadCronJobDto>
            {
                BuildCronJob(1, DomainCronJobAction.IARetrain, lastRun, nextRun, "{\"foo\":1}")
            });

        // Act
        var reply = await _service.GetEnabledCronJobs(new GetEnabledCronJobsRequest(), TestServerCallContext.Create());

        // Assert
        var job = Assert.Single(reply.CronJobs);
        Assert.Equal(1, job.IdCronjob);
        Assert.Equal("Job 1", job.NameCronjob);
        Assert.Equal("* * * * *", job.CronExpression);
        Assert.Equal(CronJobAction.IaRetrain, job.ActionCronjob);
        Assert.Equal("{\"foo\":1}", job.ParamsCronjob);
        Assert.Equal(lastRun.ToString("o"), job.LastRunAt);
        Assert.Equal(nextRun.ToString("o"), job.NextRunAt);
    }

    [Fact]
    public async Task GetEnabledCronJobs_ShouldMapNullDatesAndParams_ToEmptyStrings()
    {
        // Arrange
        _cronJobService.Setup(s => s.GetEnabledCronJobsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReadCronJobDto> { BuildCronJob(2) });

        // Act
        var reply = await _service.GetEnabledCronJobs(new GetEnabledCronJobsRequest(), TestServerCallContext.Create());

        // Assert
        var job = Assert.Single(reply.CronJobs);
        Assert.Equal(string.Empty, job.ParamsCronjob);
        Assert.Equal(string.Empty, job.LastRunAt);
        Assert.Equal(string.Empty, job.NextRunAt);
    }

    [Fact]
    public async Task UpdateCronJobRun_ShouldPassNullDates_WhenRequestFieldsBlank()
    {
        // Arrange
        var request = new UpdateCronJobRunRequest { IdCronjob = 7, LastRunAt = "", NextRunAt = "" };

        // Act
        var reply = await _service.UpdateCronJobRun(request, TestServerCallContext.Create());

        // Assert
        Assert.True(reply.Success);
        _cronJobService.Verify(s => s.UpdateCronJobRunAsync(7, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCronJobRun_ShouldParseDates_WhenRequestFieldsProvided()
    {
        // Arrange
        var lastRun = new DateTime(2026, 3, 1, 8, 30, 0, DateTimeKind.Utc);
        var nextRun = new DateTime(2026, 3, 2, 8, 30, 0, DateTimeKind.Utc);
        var request = new UpdateCronJobRunRequest
        {
            IdCronjob = 9,
            LastRunAt = lastRun.ToString("o"),
            NextRunAt = nextRun.ToString("o")
        };

        // Act
        var reply = await _service.UpdateCronJobRun(request, TestServerCallContext.Create());

        // Assert
        Assert.True(reply.Success);
        _cronJobService.Verify(s => s.UpdateCronJobRunAsync(9, lastRun, nextRun, It.IsAny<CancellationToken>()), Times.Once);
    }
}
