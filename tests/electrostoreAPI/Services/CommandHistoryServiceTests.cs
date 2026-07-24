using Microsoft.EntityFrameworkCore;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.CommandHistoryService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class CommandHistoryServiceTests : TestBase
    {
        private CommandHistoryService CreateService(ApplicationDbContext context)
        {
            return new CommandHistoryService(_mapper, context);
        }

        private static Commands BuildCommand(int id)
        {
            return new Commands
            {
                id_command = id,
                url_command = "https://example.com/order",
                tracking_number = "TRACK-" + id,
                date_command = DateTime.UtcNow
            };
        }

        private static CommandsHistory BuildCommandHistory(int id, int commandId, TrackingStatus status = TrackingStatus.InTransit)
        {
            return new CommandsHistory
            {
                id_command_history = id,
                id_command = commandId,
                status = status,
                sub_status = TrackingSubStatus.InTransit_Other
            };
        }

        // --- GetCommandHistoryByCommandId ---

        [Fact]
        public async Task GetCommandHistoryByCommandId_ShouldThrowKeyNotFoundException_WhenCommandNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetCommandHistoryByCommandId(1);
            });
        }

        [Fact]
        public async Task GetCommandHistoryByCommandId_ShouldReturnOnlyHistoryForGivenCommand()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Commands.Add(BuildCommand(2));
            context.CommandsHistory.Add(BuildCommandHistory(1, 1));
            context.CommandsHistory.Add(BuildCommandHistory(2, 1));
            context.CommandsHistory.Add(BuildCommandHistory(3, 2));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetCommandHistoryByCommandId(1);
            // Assert
            Assert.Equal(2, result.data.Count());
            Assert.All(result.data, h => Assert.Equal(1, h.id_command));
            Assert.Equal(2, result.pagination.total);
        }

        [Fact]
        public async Task GetCommandHistoryByCommandId_ShouldRespectLimitAndOffset()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            for (var i = 1; i <= 5; i++)
            {
                context.CommandsHistory.Add(BuildCommandHistory(i, 1));
            }
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetCommandHistoryByCommandId(1, limit: 2, offset: 1);
            // Assert
            Assert.Equal(2, result.data.Count());
            Assert.Equal(5, result.pagination.total);
            Assert.True(result.pagination.hasMore);
        }

        // --- GetCommandHistoryById ---

        [Fact]
        public async Task GetCommandHistoryById_ShouldReturnHistory_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.CommandsHistory.Add(BuildCommandHistory(1, 1, TrackingStatus.Delivered));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetCommandHistoryById(1, 1);
            // Assert
            Assert.Equal(1, result.id_command_history);
            Assert.Equal(TrackingStatus.Delivered, result.status);
        }

        [Fact]
        public async Task GetCommandHistoryById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetCommandHistoryById(999, 1);
            });
        }

        [Fact]
        public async Task GetCommandHistoryById_ShouldThrowKeyNotFoundException_WhenCommandIdDoesNotMatch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Commands.Add(BuildCommand(2));
            context.CommandsHistory.Add(BuildCommandHistory(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetCommandHistoryById(1, 2);
            });
        }

        // --- CreateCommandHistory ---

        [Fact]
        public async Task CreateCommandHistory_ShouldThrowKeyNotFoundException_WhenCommandNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateCommandHistoryDto { id_command = 999, status = TrackingStatus.InTransit };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreateCommandHistory(dto);
            });
        }

        [Fact]
        public async Task CreateCommandHistory_ShouldCreateHistory_WhenCommandExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateCommandHistoryDto { id_command = 1, status = TrackingStatus.Delivered, description = "Delivered to customer" };
            // Act
            var result = await service.CreateCommandHistory(dto);
            // Assert
            Assert.Equal(1, result.id_command);
            Assert.Equal(TrackingStatus.Delivered, result.status);
            Assert.Equal(1, await context.CommandsHistory.CountAsync());
        }
    }
}
