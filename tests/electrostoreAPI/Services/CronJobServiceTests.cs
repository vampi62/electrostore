using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly Mock<IKafkaProducerService> _kafkaProducer;

        public CronJobServiceTests()
        {
            _kafkaProducer = new Mock<IKafkaProducerService>();
        }

        private CronJobService CreateService(ApplicationDbContext context)
        {
            return new CronJobService(_mapper, context, _kafkaProducer.Object);
        }

        private static CronJobs BuildCronJob(int id, string name = "Job", bool isEnabled = true)
        {
            return new CronJobs
            {
                id_cronjob = id,
                name_cronjob = name,
                cron_expression = "0 0 * * *",
                action_cronjob = CronJobAction.PackageTracking,
                is_enabled = isEnabled
            };
        }

        // --- GetCronJobs ---

        [Fact]
        public async Task GetCronJobs_ShouldReturnAll_Paginated()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.CronJobs.Add(BuildCronJob(1));
            context.CronJobs.Add(BuildCronJob(2));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetCronJobs();
            // Assert
            Assert.Equal(2, result.data.Count());
            Assert.Equal(2, result.pagination.total);
        }

        [Fact]
        public async Task GetCronJobs_ShouldFilterByIdResearch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.CronJobs.Add(BuildCronJob(1));
            context.CronJobs.Add(BuildCronJob(2));
            context.CronJobs.Add(BuildCronJob(3));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetCronJobs(idResearch: new List<int> { 2 });
            // Assert
            var job = Assert.Single(result.data);
            Assert.Equal(2, job.id_cronjob);
        }

        // --- GetCronJobById ---

        [Fact]
        public async Task GetCronJobById_ShouldReturnJob_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.CronJobs.Add(BuildCronJob(1, "Tracking sync"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetCronJobById(1);
            // Assert
            Assert.Equal("Tracking sync", result.name_cronjob);
        }

        [Fact]
        public async Task GetCronJobById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetCronJobById(999);
            });
        }

        // --- CreateCronJob ---

        [Fact]
        public async Task CreateCronJob_ShouldCreateJobAndPublishEvent()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateCronJobDto { name_cronjob = "Tracking sync", cron_expression = "0 0 * * *", action_cronjob = CronJobAction.PackageTracking };
            // Act
            var result = await service.CreateCronJob(dto);
            // Assert
            Assert.Equal("Tracking sync", result.name_cronjob);
            Assert.Equal(1, await context.CronJobs.CountAsync());
            _kafkaProducer.Verify(k => k.PublishAsync("cronjob-events", It.IsAny<string>(), It.IsAny<string>(), default), Times.Once);
        }

        // --- UpdateCronJob ---

        [Fact]
        public async Task UpdateCronJob_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateCronJob(999, new UpdateCronJobDto());
            });
        }

        [Fact]
        public async Task UpdateCronJob_ShouldUpdateFieldsAndPublishEvent()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.CronJobs.Add(BuildCronJob(1, "Old name"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new UpdateCronJobDto { name_cronjob = "New name", is_enabled = false };
            // Act
            var result = await service.UpdateCronJob(1, dto);
            // Assert
            Assert.Equal("New name", result.name_cronjob);
            Assert.False(result.is_enabled);
            _kafkaProducer.Verify(k => k.PublishAsync("cronjob-events", "1", It.IsAny<string>(), default), Times.Once);
        }

        // --- DeleteCronJob ---

        [Fact]
        public async Task DeleteCronJob_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeleteCronJob(999);
            });
        }

        [Fact]
        public async Task DeleteCronJob_ShouldDeleteJobAndPublishEvent()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.CronJobs.Add(BuildCronJob(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            await service.DeleteCronJob(1);
            // Assert
            Assert.Equal(0, await context.CronJobs.CountAsync());
            _kafkaProducer.Verify(k => k.PublishAsync("cronjob-events", "1", It.IsAny<string>(), default), Times.Once);
        }

        // --- GetEnabledCronJobsAsync ---

        [Fact]
        public async Task GetEnabledCronJobsAsync_ShouldReturnOnlyEnabledJobs()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.CronJobs.Add(BuildCronJob(1, isEnabled: true));
            context.CronJobs.Add(BuildCronJob(2, isEnabled: false));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetEnabledCronJobsAsync(CancellationToken.None);
            // Assert
            var job = Assert.Single(result);
            Assert.Equal(1, job.id_cronjob);
        }

        // --- UpdateCronJobRunAsync ---

        [Fact]
        public async Task UpdateCronJobRunAsync_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateCronJobRunAsync(999, DateTime.UtcNow, DateTime.UtcNow, CancellationToken.None);
            });
        }

        [Fact]
        public async Task UpdateCronJobRunAsync_ShouldUpdateLastAndNextRun()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.CronJobs.Add(BuildCronJob(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var lastRun = DateTime.UtcNow;
            var nextRun = DateTime.UtcNow.AddDays(1);
            // Act
            await service.UpdateCronJobRunAsync(1, lastRun, nextRun, CancellationToken.None);
            // Assert
            var updated = await context.CronJobs.FindAsync(1);
            Assert.Equal(lastRun, updated!.last_run_at);
            Assert.Equal(nextRun, updated.next_run_at);
        }
    }
}
