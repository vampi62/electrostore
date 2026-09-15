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
    }
}
