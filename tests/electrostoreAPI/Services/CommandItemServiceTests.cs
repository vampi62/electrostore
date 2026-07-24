using Microsoft.EntityFrameworkCore;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.CommandItemService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class CommandItemServiceTests : TestBase
    {
        private CommandItemService CreateService(ApplicationDbContext context)
        {
            return new CommandItemService(_mapper, context);
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

        private static Items BuildItem(int id, string name = "Item")
        {
            return new Items
            {
                id_item = id,
                reference_name_item = "REF-" + id,
                friendly_name_item = name
            };
        }

        private static CommandsItems BuildCommandItem(int commandId, int itemId, int qty = 2, float price = 10f)
        {
            return new CommandsItems
            {
                id_command = commandId,
                id_item = itemId,
                qte_command_item = qty,
                prix_command_item = price
            };
        }

        // --- GetCommandsItemsByCommandId ---

        [Fact]
        public async Task GetCommandsItemsByCommandId_ShouldThrowKeyNotFoundException_WhenCommandNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetCommandsItemsByCommandId(1);
            });
        }

        [Fact]
        public async Task GetCommandsItemsByCommandId_ShouldReturnOnlyItemsForGivenCommand()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Commands.Add(BuildCommand(2));
            context.Items.Add(BuildItem(1));
            context.CommandsItems.Add(BuildCommandItem(1, 1));
            context.CommandsItems.Add(BuildCommandItem(2, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetCommandsItemsByCommandId(1);
            // Assert
            var entry = Assert.Single(result.data);
            Assert.Equal(1, entry.id_command);
        }

        // --- GetCommandsItemsByItemId ---

        [Fact]
        public async Task GetCommandsItemsByItemId_ShouldThrowKeyNotFoundException_WhenItemNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetCommandsItemsByItemId(1);
            });
        }

        [Fact]
        public async Task GetCommandsItemsByItemId_ShouldReturnOnlyCommandsForGivenItem()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Items.Add(BuildItem(1));
            context.Items.Add(BuildItem(2));
            context.CommandsItems.Add(BuildCommandItem(1, 1));
            context.CommandsItems.Add(BuildCommandItem(1, 2));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetCommandsItemsByItemId(1);
            // Assert
            var entry = Assert.Single(result.data);
            Assert.Equal(1, entry.id_item);
        }

        // --- GetCommandItemById ---

        [Fact]
        public async Task GetCommandItemById_ShouldReturnEntry_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Items.Add(BuildItem(1));
            context.CommandsItems.Add(BuildCommandItem(1, 1, 3, 15f));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetCommandItemById(1, 1);
            // Assert
            Assert.Equal(3, result.qte_command_item);
            Assert.Equal(15f, result.prix_command_item);
        }

        [Fact]
        public async Task GetCommandItemById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetCommandItemById(999, 999);
            });
        }

        // --- CreateCommandItem ---

        [Fact]
        public async Task CreateCommandItem_ShouldThrowKeyNotFoundException_WhenItemNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateCommandItemDto { id_item = 999, id_command = 1, qte_command_item = 1, prix_command_item = 5f };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreateCommandItem(dto);
            });
        }

        [Fact]
        public async Task CreateCommandItem_ShouldThrowKeyNotFoundException_WhenCommandNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateCommandItemDto { id_item = 1, id_command = 999, qte_command_item = 1, prix_command_item = 5f };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreateCommandItem(dto);
            });
        }

        [Fact]
        public async Task CreateCommandItem_ShouldThrowArgumentException_WhenAlreadyExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Items.Add(BuildItem(1));
            context.CommandsItems.Add(BuildCommandItem(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateCommandItemDto { id_item = 1, id_command = 1, qte_command_item = 1, prix_command_item = 5f };
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.CreateCommandItem(dto);
            });
        }

        [Fact]
        public async Task CreateCommandItem_ShouldCreateEntry_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Items.Add(BuildItem(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateCommandItemDto { id_item = 1, id_command = 1, qte_command_item = 4, prix_command_item = 12.5f };
            // Act
            var result = await service.CreateCommandItem(dto);
            // Assert
            Assert.Equal(4, result.qte_command_item);
            Assert.Equal(12.5f, result.prix_command_item);
            Assert.Equal(1, await context.CommandsItems.CountAsync());
        }

        // --- CreateBulkCommandItem ---

        [Fact]
        public async Task CreateBulkCommandItem_ShouldReturnMixedResults_WhenOneItemMissing()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Items.Add(BuildItem(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dtos = new List<CreateCommandItemDto>
            {
                new() { id_item = 1, id_command = 1, qte_command_item = 1, prix_command_item = 5f },
                new() { id_item = 999, id_command = 1, qte_command_item = 1, prix_command_item = 5f }
            };
            // Act
            var result = await service.CreateBulkCommandItem(dtos);
            // Assert
            Assert.Single(result.Valide);
            Assert.Single(result.Error);
        }

        // --- UpdateCommandItem ---

        [Fact]
        public async Task UpdateCommandItem_ShouldThrowKeyNotFoundException_WhenItemNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateCommandItem(999, 999, new UpdateCommandItemDto());
            });
        }

        [Fact]
        public async Task UpdateCommandItem_ShouldThrowKeyNotFoundException_WhenEntryNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Items.Add(BuildItem(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateCommandItem(1, 1, new UpdateCommandItemDto());
            });
        }

        [Fact]
        public async Task UpdateCommandItem_ShouldThrowArgumentException_WhenQuantityNotPositive()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Items.Add(BuildItem(1));
            context.CommandsItems.Add(BuildCommandItem(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.UpdateCommandItem(1, 1, new UpdateCommandItemDto { qte_command_item = 0 });
            });
        }

        [Fact]
        public async Task UpdateCommandItem_ShouldThrowArgumentException_WhenPriceNotPositive()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Items.Add(BuildItem(1));
            context.CommandsItems.Add(BuildCommandItem(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.UpdateCommandItem(1, 1, new UpdateCommandItemDto { prix_command_item = 0 });
            });
        }

        [Fact]
        public async Task UpdateCommandItem_ShouldUpdateFields_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Items.Add(BuildItem(1));
            context.CommandsItems.Add(BuildCommandItem(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.UpdateCommandItem(1, 1, new UpdateCommandItemDto { qte_command_item = 9, prix_command_item = 99f });
            // Assert
            Assert.Equal(9, result.qte_command_item);
            Assert.Equal(99f, result.prix_command_item);
        }

        // --- DeleteCommandItem ---

        [Fact]
        public async Task DeleteCommandItem_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeleteCommandItem(999, 999);
            });
        }

        [Fact]
        public async Task DeleteCommandItem_ShouldDeleteEntry_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Items.Add(BuildItem(1));
            context.CommandsItems.Add(BuildCommandItem(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            await service.DeleteCommandItem(1, 1);
            // Assert
            Assert.Equal(0, await context.CommandsItems.CountAsync());
        }
    }
}
