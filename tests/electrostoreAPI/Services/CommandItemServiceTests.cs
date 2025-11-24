using Microsoft.EntityFrameworkCore;
using Xunit;
using electrostore;
using electrostore.Dto;
using electrostore.Models;
using electrostore.Services.CommandItemService;
using electrostore.Tests.Utils;

namespace electrostore.Tests.Services
{
    public class CommandItemServiceTests : TestBase
    {
        [Fact]
        public async Task GetByCommand_ShouldReturnItems_AndCount()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            // Seed items and command
            for (int i = 1; i <= 3; i++)
            {
                context.Items.Add(new Items
                {
                    id_item = i,
                    reference_name_item = $"REF-{i}",
                    friendly_name_item = $"Item {i}",
                    description_item = $"Desc {i}",
                    seuil_min_item = 1
                });
            }
            context.Commands.Add(new Commands
            {
                id_command = 1,
                prix_command = 100,
                url_command = "https://ex.com/c/1",
                status_command = "pending",
                date_command = DateTime.UtcNow
            });
            // link items to command
            for (int i = 1; i <= 3; i++)
            {
                context.CommandsItems.Add(new CommandsItems
                {
                    id_command = 1,
                    id_item = i,
                    qte_command_item = 1 + i,
                    prix_command_item = 10 * i
                });
            }
            await context.SaveChangesAsync();

            var commandItemService = new CommandItemService(_mapper, context);

            var list = await commandItemService.GetCommandsItemsByCommandId(1, 10, 0);
            var count = await commandItemService.GetCommandsItemsCountByCommandId(1);

            Assert.NotNull(list);
            Assert.Equal(3, list.Count());
            Assert.Equal(3, count);
        }

        [Fact]
        public async Task GetByItem_ShouldReturnCommands_AndCount()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            // Seed item and commands
            context.Items.Add(new Items
            {
                id_item = 10,
                reference_name_item = "REF-10",
                friendly_name_item = "Item 10",
                description_item = "Desc 10",
                seuil_min_item = 1
            });
            for (int c = 1; c <= 2; c++)
            {
                context.Commands.Add(new Commands
                {
                    id_command = c,
                    prix_command = 50 + c,
                    url_command = $"https://ex.com/c/{c}",
                    status_command = "pending",
                    date_command = DateTime.UtcNow
                });
                context.CommandsItems.Add(new CommandsItems
                {
                    id_command = c,
                    id_item = 10,
                    qte_command_item = 2,
                    prix_command_item = 20
                });
            }
            await context.SaveChangesAsync();

            var commandItemService = new CommandItemService(_mapper, context);

            var list = await commandItemService.GetCommandsItemsByItemId(10, 10, 0);
            var count = await commandItemService.GetCommandsItemsCountByItemId(10);

            Assert.NotNull(list);
            Assert.Equal(2, list.Count());
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task GetById_ShouldReturnSingleCommandItem()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Items.Add(new Items
            {
                id_item = 20,
                reference_name_item = "REF-20",
                friendly_name_item = "Item 20",
                description_item = "Desc 20",
                seuil_min_item = 1
            });
            context.Commands.Add(new Commands
            {
                id_command = 5,
                prix_command = 80,
                url_command = "https://ex.com/c/5",
                status_command = "pending",
                date_command = DateTime.UtcNow
            });
            context.CommandsItems.Add(new CommandsItems
            {
                id_command = 5,
                id_item = 20,
                qte_command_item = 3,
                prix_command_item = 9.5f
            });
            await context.SaveChangesAsync();

            var commandItemService = new CommandItemService(_mapper, context);
            var ci = await commandItemService.GetCommandItemById(5, 20);

            Assert.NotNull(ci);
            Assert.Equal(5, ci.id_command);
            Assert.Equal(20, ci.id_item);
            Assert.Equal(3, ci.qte_command_item);
            Assert.Equal(9.5f, ci.prix_command_item);
        }

        [Fact]
        public async Task Create_ShouldCreateCommandItem()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Items.Add(new Items
            {
                id_item = 30,
                reference_name_item = "REF-30",
                friendly_name_item = "Item 30",
                description_item = "Desc 30",
                seuil_min_item = 1
            });
            context.Commands.Add(new Commands
            {
                id_command = 7,
                prix_command = 20,
                url_command = "https://ex.com/c/7",
                status_command = "pending",
                date_command = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var commandItemService = new CommandItemService(_mapper, context);

            var dto = new CreateCommandItemDto
            {
                id_command = 7,
                id_item = 30,
                qte_command_item = 5,
                prix_command_item = 12.3f
            };

            var created = await commandItemService.CreateCommandItem(dto);

            Assert.NotNull(created);
            Assert.Equal(7, created.id_command);
            Assert.Equal(30, created.id_item);
            Assert.Equal(5, created.qte_command_item);
            Assert.Equal(12.3f, created.prix_command_item);

            var inDb = await context.CommandsItems.FindAsync(7, 30);
            Assert.NotNull(inDb);
        }

        [Fact]
        public async Task CreateBulk_ShouldReturnValidAndErrorLists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            // Seed two items and one command; also pre-create a pair to cause duplicate
            context.Items.AddRange(
                new Items { id_item = 40, reference_name_item = "R40", friendly_name_item = "I40", description_item = "D40", seuil_min_item = 1 },
                new Items { id_item = 41, reference_name_item = "R41", friendly_name_item = "I41", description_item = "D41", seuil_min_item = 1 }
            );
            context.Commands.Add(new Commands { id_command = 9, prix_command = 10, url_command = "https://ex.com/c/9", status_command = "pending", date_command = DateTime.UtcNow });
            context.CommandsItems.Add(new CommandsItems { id_command = 9, id_item = 41, qte_command_item = 1, prix_command_item = 1 }); // duplicate case
            await context.SaveChangesAsync();

            var commandItemService = new CommandItemService(_mapper, context);

            var bulk = new List<CreateCommandItemDto>
            {
                new() { id_command = 9, id_item = 40, qte_command_item = 2, prix_command_item = 3.3f }, // valid
                new() { id_command = 9, id_item = 41, qte_command_item = 2, prix_command_item = 3.3f }  // duplicate -> error
            };

            var result = await commandItemService.CreateBulkCommandItem(bulk);

            Assert.NotNull(result);
            Assert.Single(result.Valide);
            Assert.Single(result.Error);
            Assert.Equal(40, result.Valide[0].id_item);
        }

        [Fact]
        public async Task Update_ShouldModifyQteAndPrix()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Items.Add(new Items { id_item = 50, reference_name_item = "R50", friendly_name_item = "I50", description_item = "D50", seuil_min_item = 1 });
            context.Commands.Add(new Commands { id_command = 11, prix_command = 10, url_command = "https://ex.com/c/11", status_command = "pending", date_command = DateTime.UtcNow });
            context.CommandsItems.Add(new CommandsItems { id_command = 11, id_item = 50, qte_command_item = 1, prix_command_item = 1.0f });
            await context.SaveChangesAsync();

            var commandItemService = new CommandItemService(_mapper, context);

            var updated = await commandItemService.UpdateCommandItem(11, 50, new UpdateCommandItemDto
            {
                qte_command_item = 4,
                prix_command_item = 7.7f
            });

            Assert.NotNull(updated);
            Assert.Equal(4, updated.qte_command_item);
            Assert.Equal(7.7f, updated.prix_command_item);

            var inDb = await context.CommandsItems.FindAsync(11, 50);
            Assert.NotNull(inDb);
            Assert.Equal(4, inDb!.qte_command_item);
            Assert.Equal(7.7f, inDb!.prix_command_item);
        }

        [Fact]
        public async Task Delete_ShouldRemoveCommandItem()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Items.Add(new Items { id_item = 60, reference_name_item = "R60", friendly_name_item = "I60", description_item = "D60", seuil_min_item = 1 });
            context.Commands.Add(new Commands { id_command = 12, prix_command = 10, url_command = "https://ex.com/c/12", status_command = "pending", date_command = DateTime.UtcNow });
            context.CommandsItems.Add(new CommandsItems { id_command = 12, id_item = 60, qte_command_item = 1, prix_command_item = 1 });
            await context.SaveChangesAsync();

            var commandItemService = new CommandItemService(_mapper, context);

            await commandItemService.DeleteCommandItem(12, 60);

            var inDb = await context.CommandsItems.FindAsync(12, 60);
            Assert.Null(inDb);
        }
    }
}