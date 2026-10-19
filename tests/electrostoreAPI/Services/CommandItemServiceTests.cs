using Microsoft.EntityFrameworkCore;
using Xunit;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.CommandItemService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class CommandItemServiceTests : TestBase
    {
        private CommandItemService CreateService(ApplicationDbContext context) =>
            new(_mapper, context);

        private static Commands BuildCommand(string tag = "command") => new()
        {
            url_command = tag,
            date_command = DateTime.UtcNow
        };

        private static Items BuildItem(string reference = "item") => new()
        {
            reference_name_item = reference,
            friendly_name_item = reference,
            threshold_min_item = 0
        };

        private static CommandsItems BuildCommandItem(int parentId, int itemId, int quantity = 1) => new()
        {
            id_command = parentId,
            id_item = itemId,
            quantity_command_item = quantity,
            price_command_item = 1f
        };

        private static CreateCommandItemDto BuildCreateDto(int parentId, int itemId, int quantity = 2) => new()
        {
            id_command = parentId,
            id_item = itemId,
            quantity_command_item = quantity,
            price_command_item = 3f
        };

        private async Task<(Commands parent, Items item)> Seed(ApplicationDbContext context, bool link = false)
        {
            var parent = BuildCommand();
            var item = BuildItem();
            context.Commands.Add(parent);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            if (link)
            {
                context.CommandsItems.Add(BuildCommandItem(parent.id_command, item.id_item));
                await context.SaveChangesAsync();
            }
            return (parent, item);
        }

        // --- GetCommandsItemsByCommandId ---

        [Fact]
        public async Task GetCommandsItemsByCommandId_ShouldReturnOnlyItemsOfParent()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, item) = await Seed(context, link: true);
            var other = BuildCommand("other");
            var otherItem = BuildItem("other");
            context.Commands.Add(other);
            context.Items.Add(otherItem);
            await context.SaveChangesAsync();
            context.CommandsItems.Add(BuildCommandItem(other.id_command, otherItem.id_item));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetCommandsItemsByCommandId(parent.id_command);

            var single = Assert.Single(result.data);
            Assert.Equal(item.id_item, single.id_item);
        }

        [Fact]
        public async Task GetCommandsItemsByCommandId_ShouldExpandItemAndParent_WhenRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, _) = await Seed(context, link: true);
            var service = CreateService(context);

            var result = await service.GetCommandsItemsByCommandId(parent.id_command, expand: ["item", "command"]);

            var single = Assert.Single(result.data);
            Assert.NotNull(single.item);
            Assert.NotNull(single.command);
        }

        [Fact]
        public async Task GetCommandsItemsByCommandId_ShouldThrowKeyNotFoundException_WhenParentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetCommandsItemsByCommandId(999));
        }

        // --- GetCommandsItemsByItemId ---

        [Fact]
        public async Task GetCommandsItemsByItemId_ShouldReturnParentsLinkedToItem()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, item) = await Seed(context, link: true);
            var service = CreateService(context);

            var result = await service.GetCommandsItemsByItemId(item.id_item);

            var single = Assert.Single(result.data);
            Assert.Equal(parent.id_command, single.id_command);
        }

        [Fact]
        public async Task GetCommandsItemsByItemId_ShouldExpandItemAndParent_WhenRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (_, item) = await Seed(context, link: true);
            var service = CreateService(context);

            var result = await service.GetCommandsItemsByItemId(item.id_item, expand: ["item", "command"]);

            var single = Assert.Single(result.data);
            Assert.NotNull(single.item);
            Assert.NotNull(single.command);
        }

        [Fact]
        public async Task GetCommandsItemsByItemId_ShouldThrowKeyNotFoundException_WhenItemDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetCommandsItemsByItemId(999));
        }

        // --- GetCommandItemById ---

        [Fact]
        public async Task GetCommandItemById_ShouldReturnLink_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, item) = await Seed(context, link: true);
            var service = CreateService(context);

            var result = await service.GetCommandItemById(parent.id_command, item.id_item);

            Assert.Equal(1, result.quantity_command_item);
        }

        [Fact]
        public async Task GetCommandItemById_ShouldExpandItemAndParent_WhenRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, item) = await Seed(context, link: true);
            var service = CreateService(context);

            var result = await service.GetCommandItemById(parent.id_command, item.id_item, ["item", "command"]);

            Assert.NotNull(result.item);
            Assert.NotNull(result.command);
        }

        [Fact]
        public async Task GetCommandItemById_ShouldThrowKeyNotFoundException_WhenLinkDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetCommandItemById(1, 1));
        }

        // --- CreateCommandItem ---

        [Fact]
        public async Task CreateCommandItem_ShouldPersistLink()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, item) = await Seed(context);
            var service = CreateService(context);

            var result = await service.CreateCommandItem(BuildCreateDto(parent.id_command, item.id_item, 5));

            Assert.Equal(5, result.quantity_command_item);
            Assert.Equal(1, await context.CommandsItems.CountAsync());
        }

        [Fact]
        public async Task CreateCommandItem_ShouldThrowKeyNotFoundException_WhenParentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem();
            context.Items.Add(item);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateCommandItem(BuildCreateDto(999, item.id_item)));
        }

        [Fact]
        public async Task CreateCommandItem_ShouldThrowKeyNotFoundException_WhenItemDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var parent = BuildCommand();
            context.Commands.Add(parent);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateCommandItem(BuildCreateDto(parent.id_command, 999)));
        }

        [Fact]
        public async Task CreateCommandItem_ShouldThrowArgumentException_WhenLinkAlreadyExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, item) = await Seed(context, link: true);
            var service = CreateService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCommandItem(BuildCreateDto(parent.id_command, item.id_item)));
        }

        // --- CreateBulkCommandItem ---

        [Fact]
        public async Task CreateBulkCommandItem_ShouldReturnValidAndErrorEntries()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, item) = await Seed(context);
            var service = CreateService(context);

            var result = await service.CreateBulkCommandItem(
            [
                BuildCreateDto(parent.id_command, item.id_item),
                BuildCreateDto(parent.id_command, item.id_item),
                BuildCreateDto(999, item.id_item)
            ]);

            Assert.Single(result.valide);
            Assert.Equal(2, result.error.Count());
            Assert.Equal(1, await context.CommandsItems.CountAsync());
        }

        // --- UpdateCommandItem ---

        [Fact]
        public async Task UpdateCommandItem_ShouldUpdateQuantity()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, item) = await Seed(context, link: true);
            var service = CreateService(context);

            var result = await service.UpdateCommandItem(parent.id_command, item.id_item, new UpdateCommandItemDto { quantity_command_item = 9 });

            Assert.Equal(9, result.quantity_command_item);
        }

        [Fact]
        public async Task UpdateCommandItem_ShouldKeepValues_WhenDtoIsEmpty()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, item) = await Seed(context, link: true);
            var service = CreateService(context);

            var result = await service.UpdateCommandItem(parent.id_command, item.id_item, new UpdateCommandItemDto());

            Assert.Equal(1, result.quantity_command_item);
        }

        [Fact]
        public async Task UpdateCommandItem_ShouldThrowKeyNotFoundException_WhenParentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (_, item) = await Seed(context);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateCommandItem(999, item.id_item, new UpdateCommandItemDto()));
        }

        [Fact]
        public async Task UpdateCommandItem_ShouldThrowKeyNotFoundException_WhenItemDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, _) = await Seed(context);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateCommandItem(parent.id_command, 999, new UpdateCommandItemDto()));
        }

        [Fact]
        public async Task UpdateCommandItem_ShouldThrowKeyNotFoundException_WhenLinkDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, item) = await Seed(context);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateCommandItem(parent.id_command, item.id_item, new UpdateCommandItemDto()));
        }

        [Fact]
        public async Task UpdateCommandItem_ShouldUpdatePrice()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (command, item) = await Seed(context, link: true);
            var service = CreateService(context);

            var result = await service.UpdateCommandItem(command.id_command, item.id_item, new UpdateCommandItemDto { price_command_item = 12.5f });

            Assert.Equal(12.5f, result.price_command_item);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public async Task UpdateCommandItem_ShouldThrowArgumentException_WhenQuantityIsNotPositive(int quantity)
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (command, item) = await Seed(context, link: true);
            var service = CreateService(context);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.UpdateCommandItem(command.id_command, item.id_item, new UpdateCommandItemDto { quantity_command_item = quantity }));
        }

        [Fact]
        public async Task UpdateCommandItem_ShouldThrowArgumentException_WhenPriceIsNegative()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (command, item) = await Seed(context, link: true);
            var service = CreateService(context);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.UpdateCommandItem(command.id_command, item.id_item, new UpdateCommandItemDto { price_command_item = -1f }));
        }

        // --- DeleteCommandItem ---

        [Fact]
        public async Task DeleteCommandItem_ShouldRemoveLink()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, item) = await Seed(context, link: true);
            var service = CreateService(context);

            await service.DeleteCommandItem(parent.id_command, item.id_item);

            Assert.Equal(0, await context.CommandsItems.CountAsync());
        }

        [Fact]
        public async Task DeleteCommandItem_ShouldThrowKeyNotFoundException_WhenLinkDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteCommandItem(1, 1));
        }
    }
}
