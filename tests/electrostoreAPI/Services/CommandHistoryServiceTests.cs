using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.CommandHistoryService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class CommandHistoryServiceTests : TestBase
    {
        private CommandHistoryService CreateService(ApplicationDbContext context) =>
            new(_mapper, context);

        private static Commands BuildCommand() => new()
        {
            url_command = "https://example.com",
            date_command = DateTime.UtcNow
        };

        private static CommandsHistory BuildHistory(int commandId, string description = "event") => new()
        {
            id_command = commandId,
            description_command_history = description
        };

        // --- GetCommandHistoryByCommandId ---

        [Fact]
        public async Task GetCommandHistoryByCommandId_ShouldReturnOnlyHistoryOfCommand()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var command = BuildCommand();
            var other = BuildCommand();
            context.Commands.AddRange(command, other);
            await context.SaveChangesAsync();
            context.CommandsHistory.AddRange(BuildHistory(command.id_command, "a"), BuildHistory(command.id_command, "b"), BuildHistory(other.id_command, "c"));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetCommandHistoryByCommandId(command.id_command);

            Assert.Equal(2, result.data.Count());
            Assert.All(result.data, h => Assert.Equal(command.id_command, h.id_command));
        }

        [Fact]
        public async Task GetCommandHistoryByCommandId_ShouldThrowKeyNotFoundException_WhenCommandDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetCommandHistoryByCommandId(999));
        }

        // --- GetCommandHistoryById ---

        [Fact]
        public async Task GetCommandHistoryById_ShouldReturnHistory_WhenItBelongsToCommand()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var command = BuildCommand();
            context.Commands.Add(command);
            await context.SaveChangesAsync();
            var history = BuildHistory(command.id_command);
            context.CommandsHistory.Add(history);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetCommandHistoryById(history.id_command_history, command.id_command);

            Assert.Equal(history.id_command_history, result.id_command_history);
            Assert.Equal("event", result.description_command_history);
        }

        [Fact]
        public async Task GetCommandHistoryById_ShouldThrowKeyNotFoundException_WhenCommandDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var command = BuildCommand();
            context.Commands.Add(command);
            await context.SaveChangesAsync();
            var history = BuildHistory(command.id_command);
            context.CommandsHistory.Add(history);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetCommandHistoryById(history.id_command_history, command.id_command + 1));
        }

        [Fact]
        public async Task GetCommandHistoryById_ShouldThrowKeyNotFoundException_WhenHistoryDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetCommandHistoryById(999, 1));
        }

        // --- CreateCommandHistory ---

        [Fact]
        public async Task CreateCommandHistory_ShouldPersistHistory()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var command = BuildCommand();
            context.Commands.Add(command);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.CreateCommandHistory(new CreateCommandHistoryDto
            {
                id_command = command.id_command,
                status_command_history = TrackingStatus.InTransit,
                description_command_history = "left warehouse",
                city_command_history = "Paris"
            });

            Assert.Equal(command.id_command, result.id_command);
            Assert.Equal("left warehouse", result.description_command_history);
            Assert.Equal(1, await context.CommandsHistory.CountAsync());
        }

        [Fact]
        public async Task CreateCommandHistory_ShouldThrowKeyNotFoundException_WhenCommandDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateCommandHistory(new CreateCommandHistoryDto { id_command = 999 }));
        }
    }
}
