using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Kafka.Producer;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.CronJobService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class CronJobServiceTests : TestBase
    {
        private readonly Mock<IKafkaProducerService> _kafkaProducerService = new();

        private CronJobService CreateService(ApplicationDbContext context) =>
            new(_mapper, context, _kafkaProducerService.Object);

        private static CronJobs BuildCronJob(string name = "job") => new()
        {
            name_cronjob = name,
            cron_expression_cronjob = "* * * * *",
            action_cronjob = CronJobAction.StockLowAlert
        };

        // --- GetCronJobs ---

        [Fact]
        public async Task GetCronJobs_ShouldReturnAllCronJobs_WhenNoFilterApplied()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.CronJobs.AddRange(BuildCronJob("a"), BuildCronJob("b"));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetCronJobs();

            Assert.Equal(2, result.pagination.total);
            Assert.Equal(2, result.data.Count());
        }

        [Fact]
        public async Task GetCronJobs_ShouldRespectLimitAndOffset()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.CronJobs.AddRange(BuildCronJob("a"), BuildCronJob("b"), BuildCronJob("c"));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetCronJobs(limit: 1, offset: 1);

            Assert.Single(result.data);
            Assert.Equal("b", result.data.First().name_cronjob);
            Assert.True(result.pagination.has_more);
        }

        // --- GetCronJobById ---

        [Fact]
        public async Task GetCronJobById_ShouldReturnCronJob_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var job = BuildCronJob();
            context.CronJobs.Add(job);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetCronJobById(job.id_cronjob);

            Assert.Equal(job.id_cronjob, result.id_cronjob);
            Assert.Equal("job", result.name_cronjob);
        }

        [Fact]
        public async Task GetCronJobById_ShouldThrowKeyNotFoundException_WhenCronJobDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetCronJobById(999));
        }

        // --- CreateCronJob ---

        [Fact]
        public async Task CreateCronJob_ShouldPersistCronJob_AndPublishCreatedEvent()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateCronJobDto
            {
                name_cronjob = "new-job",
                cron_expression_cronjob = "0 * * * *",
                action_cronjob = CronJobAction.PackageTracking
            };

            var result = await service.CreateCronJob(dto);

            Assert.Equal("new-job", result.name_cronjob);
            Assert.Equal(1, await context.CronJobs.CountAsync());
            _kafkaProducerService.Verify(k => k.PublishAsync(
                "cronjob-events", result.id_cronjob.ToString(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        // --- UpdateCronJob ---

        [Fact]
        public async Task UpdateCronJob_ShouldUpdateProvidedFields_AndPublishUpdatedEvent()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var job = BuildCronJob();
            context.CronJobs.Add(job);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new UpdateCronJobDto { name_cronjob = "renamed", is_enabled = false };

            var result = await service.UpdateCronJob(job.id_cronjob, dto);

            Assert.Equal("renamed", result.name_cronjob);
            Assert.False(result.is_enabled);
            Assert.Equal("* * * * *", result.cron_expression_cronjob);
            _kafkaProducerService.Verify(k => k.PublishAsync(
                "cronjob-events", job.id_cronjob.ToString(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateCronJob_ShouldThrowKeyNotFoundException_WhenCronJobDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateCronJob(999, new UpdateCronJobDto()));
        }

        // --- DeleteCronJob ---

        [Fact]
        public async Task DeleteCronJob_ShouldRemoveCronJob_AndPublishDeletedEvent()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var job = BuildCronJob();
            context.CronJobs.Add(job);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.DeleteCronJob(job.id_cronjob);

            Assert.Equal(0, await context.CronJobs.CountAsync());
            _kafkaProducerService.Verify(k => k.PublishAsync(
                "cronjob-events", job.id_cronjob.ToString(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteCronJob_ShouldThrowKeyNotFoundException_WhenCronJobDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteCronJob(999));
        }

        // --- GetCronJobs: eq filter ---

        [Fact]
        public async Task GetCronJobs_ShouldRestrictToIds_WhenAnyFilterProvided()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var a = BuildCronJob("a");
            var b = BuildCronJob("b");
            context.CronJobs.AddRange(a, b);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetCronJobs(rsql: [new FilterDto { field = "id_cronjob", search_type = "eq", value = b.id_cronjob.ToString() }]);

            var single = Assert.Single(result.data);
            Assert.Equal(b.id_cronjob, single.id_cronjob);
        }

        // --- UpdateCronJob: every field ---

        [Fact]
        public async Task UpdateCronJob_ShouldUpdateEveryField()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var job = BuildCronJob("old");
            context.CronJobs.Add(job);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var lastRun = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var nextRun = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);

            var result = await service.UpdateCronJob(job.id_cronjob, new UpdateCronJobDto
            {
                name_cronjob = "new",
                cron_expression_cronjob = "0 8 * * *",
                action_cronjob = CronJobAction.WeeklyItemMovementReport,
                params_cronjob = "{\"a\":1}",
                is_enabled = false,
                last_run_at = lastRun,
                next_run_at = nextRun
            });

            Assert.Equal("new", result.name_cronjob);
            Assert.Equal("0 8 * * *", result.cron_expression_cronjob);
            Assert.Equal(CronJobAction.WeeklyItemMovementReport, result.action_cronjob);
            Assert.Equal("{\"a\":1}", result.params_cronjob);
            Assert.False(result.is_enabled);
            Assert.Equal(lastRun, result.last_run_at);
            Assert.Equal(nextRun, result.next_run_at);
        }

        // --- GetEnabledCronJobsAsync ---

        [Fact]
        public async Task GetEnabledCronJobsAsync_ShouldReturnOnlyEnabledJobs()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var enabled = BuildCronJob("enabled");
            enabled.is_enabled = true;
            var disabled = BuildCronJob("disabled");
            disabled.is_enabled = false;
            context.CronJobs.AddRange(enabled, disabled);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetEnabledCronJobsAsync(CancellationToken.None);

            var single = Assert.Single(result);
            Assert.Equal("enabled", single.name_cronjob);
        }

        // --- UpdateCronJobRunAsync ---

        [Fact]
        public async Task UpdateCronJobRunAsync_ShouldUpdateProvidedDates()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var job = BuildCronJob();
            context.CronJobs.Add(job);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var lastRun = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
            var nextRun = new DateTime(2026, 3, 2, 10, 0, 0, DateTimeKind.Utc);

            await service.UpdateCronJobRunAsync(job.id_cronjob, lastRun, nextRun, CancellationToken.None);

            var reloaded = await context.CronJobs.FindAsync(job.id_cronjob);
            Assert.Equal(lastRun, reloaded!.last_run_at);
            Assert.Equal(nextRun, reloaded.next_run_at);
        }

        [Fact]
        public async Task UpdateCronJobRunAsync_ShouldKeepExistingDates_WhenNullProvided()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var job = BuildCronJob();
            var lastRun = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
            var nextRun = new DateTime(2026, 3, 2, 10, 0, 0, DateTimeKind.Utc);
            job.last_run_at = lastRun;
            job.next_run_at = nextRun;
            context.CronJobs.Add(job);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.UpdateCronJobRunAsync(job.id_cronjob, null, null, CancellationToken.None);

            var reloaded = await context.CronJobs.FindAsync(job.id_cronjob);
            Assert.Equal(lastRun, reloaded!.last_run_at);
            Assert.Equal(nextRun, reloaded.next_run_at);
        }

        [Fact]
        public async Task UpdateCronJobRunAsync_ShouldThrowKeyNotFoundException_WhenCronJobDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateCronJobRunAsync(999, null, null, CancellationToken.None));
        }

        // --- UpdateCronJobStatusAsync ---

        [Fact]
        public async Task UpdateCronJobStatusAsync_ShouldStoreStatusAndError()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var job = BuildCronJob();
            context.CronJobs.Add(job);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.UpdateCronJobStatusAsync(job.id_cronjob, CronJobStatus.Failed, "boom", CancellationToken.None);

            var reloaded = await context.CronJobs.FindAsync(job.id_cronjob);
            Assert.Equal(CronJobStatus.Failed, reloaded!.status_cronjob);
            Assert.Equal("boom", reloaded.last_error_cronjob);
        }

        [Fact]
        public async Task UpdateCronJobStatusAsync_ShouldClearError_WhenNullProvided()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var job = BuildCronJob();
            job.status_cronjob = CronJobStatus.Failed;
            job.last_error_cronjob = "old error";
            context.CronJobs.Add(job);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.UpdateCronJobStatusAsync(job.id_cronjob, CronJobStatus.Success, null, CancellationToken.None);

            var reloaded = await context.CronJobs.FindAsync(job.id_cronjob);
            Assert.Equal(CronJobStatus.Success, reloaded!.status_cronjob);
            Assert.Null(reloaded.last_error_cronjob);
        }

        [Fact]
        public async Task UpdateCronJobStatusAsync_ShouldThrowKeyNotFoundException_WhenCronJobDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateCronJobStatusAsync(999, CronJobStatus.Running, null, CancellationToken.None));
        }

        // --- GetCronJobStatus ---

        [Fact]
        public async Task GetCronJobStatus_ShouldReturnStatusFields()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var job = BuildCronJob();
            job.status_cronjob = CronJobStatus.Running;
            job.last_error_cronjob = "previous";
            job.last_run_at = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            job.next_run_at = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
            context.CronJobs.Add(job);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetCronJobStatus(job.id_cronjob);

            Assert.Equal(job.id_cronjob, result.id_cronjob);
            Assert.Equal(CronJobStatus.Running, result.status_cronjob);
            Assert.Equal("previous", result.last_error_cronjob);
            Assert.Equal(job.last_run_at, result.last_run_at);
            Assert.Equal(job.next_run_at, result.next_run_at);
        }

        [Fact]
        public async Task GetCronJobStatus_ShouldThrowKeyNotFoundException_WhenCronJobDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetCronJobStatus(999));
        }

        // --- ForceRunCronJob ---

        [Fact]
        public async Task ForceRunCronJob_ShouldPublishForceRunEvent_WhenJobIsEnabled()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var job = BuildCronJob();
            job.is_enabled = true;
            context.CronJobs.Add(job);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.ForceRunCronJob(job.id_cronjob);

            _kafkaProducerService.Verify(k => k.PublishAsync("cronjob-events", job.id_cronjob.ToString(),
                It.Is<string>(m => m.Contains("force_run")), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ForceRunCronJob_ShouldThrowInvalidOperationException_WhenJobIsDisabled()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var job = BuildCronJob();
            job.is_enabled = false;
            context.CronJobs.Add(job);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ForceRunCronJob(job.id_cronjob));
            _kafkaProducerService.Verify(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ForceRunCronJob_ShouldThrowKeyNotFoundException_WhenCronJobDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ForceRunCronJob(999));
        }

        // --- ForceStopCronJob ---

        [Fact]
        public async Task ForceStopCronJob_ShouldPublishForceStopEvent()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var job = BuildCronJob();
            context.CronJobs.Add(job);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.ForceStopCronJob(job.id_cronjob);

            _kafkaProducerService.Verify(k => k.PublishAsync("cronjob-events", job.id_cronjob.ToString(),
                It.Is<string>(m => m.Contains("force_stop")), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ForceStopCronJob_ShouldThrowKeyNotFoundException_WhenCronJobDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ForceStopCronJob(999));
        }
    }
}
