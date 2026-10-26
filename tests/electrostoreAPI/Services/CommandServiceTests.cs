using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.CommandService;
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class CommandServiceTests : TestBase
    {
        private readonly Mock<IFileService> _fileService = new();

        private CommandService CreateService(ApplicationDbContext context) =>
            new(_mapper, context, _fileService.Object);

        private static Commands BuildCommand(string url = "https://example.com") => new()
        {
            url_command = url,
            date_command = DateTime.UtcNow
        };

        private static Carriers BuildCarrier(int key = 1, string name = "carrier") => new()
        {
            key_carrier = key,
            name_carrier = name
        };

        private static Items BuildItem(string reference = "item") => new()
        {
            reference_name_item = reference,
            friendly_name_item = reference,
            threshold_min_item = 0
        };

        // --- GetCommands ---

        [Fact]
        public async Task GetCommands_ShouldReturnAllCommands_WhenNoFilterApplied()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.AddRange(BuildCommand("a"), BuildCommand("b"));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetCommands();

            Assert.Equal(2, result.pagination.total);
            Assert.Equal(2, result.data.Count());
        }

        [Fact]
        public async Task GetCommands_ShouldRestrictToIds_WhenAnyFilterProvided()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var a = BuildCommand("a");
            var b = BuildCommand("b");
            context.Commands.AddRange(a, b);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetCommands(rsql: [new FilterDto { field = "id_command", search_type = "eq", value = a.id_command.ToString() }]);

            var single = Assert.Single(result.data);
            Assert.Equal(a.id_command, single.id_command);
        }

        [Fact]
        public async Task GetCommands_ShouldReturnRelatedEntities_WhenExpanded()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var carrier = BuildCarrier();
            var item = BuildItem();
            context.Carriers.Add(carrier);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            var command = BuildCommand();
            command.id_carrier = carrier.id_carrier;
            context.Commands.Add(command);
            await context.SaveChangesAsync();
            context.CommandsItems.Add(new CommandsItems { id_command = command.id_command, id_item = item.id_item, quantity_command_item = 1, price_command_item = 2f });
            context.CommandsComments.Add(new CommandsComments { id_command = command.id_command, content_command_comment = "hi" });
            context.CommandsDocuments.Add(new CommandsDocuments
            {
                id_command = command.id_command,
                url_command_document = "c/doc.pdf",
                name_command_document = "doc",
                type_command_document = "application/pdf",
                size_command_document = 1
            });
            context.CommandsHistory.Add(new CommandsHistory { id_command = command.id_command, description_command_history = "shipped" });
            await context.SaveChangesAsync();
            using var queryContext = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(queryContext);

            var result = await service.GetCommands(expand: ["command_comments", "commands_documents", "commands_history", "commands_items", "carrier"]);

            var single = Assert.Single(result.data);
            Assert.Equal(1, single.command_comments_count);
            Assert.Equal(1, single.commands_documents_count);
            Assert.Equal(1, single.commands_items_count);
            Assert.Single(single.command_comments!);
            Assert.Single(single.commands_documents!);
            Assert.Single(single.commands_history!);
            Assert.Single(single.commands_items!);
            Assert.NotNull(single.carrier);
        }

        // --- GetCommandById ---

        [Fact]
        public async Task GetCommandById_ShouldReturnCommand_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var command = BuildCommand();
            context.Commands.Add(command);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetCommandById(command.id_command);

            Assert.Equal(command.id_command, result.id_command);
            Assert.Equal(0, result.commands_items_count);
            Assert.Null(result.carrier);
        }

        [Fact]
        public async Task GetCommandById_ShouldReturnRelatedEntities_WhenExpanded()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var carrier = BuildCarrier();
            context.Carriers.Add(carrier);
            await context.SaveChangesAsync();
            var command = BuildCommand();
            command.id_carrier = carrier.id_carrier;
            context.Commands.Add(command);
            await context.SaveChangesAsync();
            context.CommandsComments.Add(new CommandsComments { id_command = command.id_command, content_command_comment = "hi" });
            context.CommandsHistory.Add(new CommandsHistory { id_command = command.id_command, description_command_history = "shipped" });
            await context.SaveChangesAsync();
            using var queryContext = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(queryContext);

            var result = await service.GetCommandById(command.id_command, ["command_comments", "commands_documents", "commands_history", "commands_items", "carrier"]);

            Assert.Single(result.command_comments!);
            Assert.Single(result.commands_history!);
            Assert.Empty(result.commands_documents!);
            Assert.Empty(result.commands_items!);
            Assert.NotNull(result.carrier);
        }

        [Fact]
        public async Task GetCommandById_ShouldThrowKeyNotFoundException_WhenCommandDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetCommandById(999));
        }

        // --- CreateCommand ---

        [Fact]
        public async Task CreateCommand_ShouldPersistCommand_AndCreateDirectory()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            var result = await service.CreateCommand(new CreateCommandDto
            {
                url_command = "https://shop.example.com/order/1",
                date_command = DateTime.UtcNow,
                price_command = 12.5f
            });

            Assert.Equal("https://shop.example.com/order/1", result.url_command);
            Assert.Equal(1, await context.Commands.CountAsync());
            _fileService.Verify(f => f.CreateDirectory(It.Is<string>(p => p.StartsWith("commandDocuments"))), Times.Once);
        }

        [Fact]
        public async Task CreateCommand_ShouldPersistCommand_WhenCarrierExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var carrier = BuildCarrier();
            context.Carriers.Add(carrier);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.CreateCommand(new CreateCommandDto { date_command = DateTime.UtcNow, id_carrier = carrier.id_carrier });

            Assert.Equal(carrier.id_carrier, result.id_carrier);
        }

        [Fact]
        public async Task CreateCommand_ShouldThrowKeyNotFoundException_WhenCarrierDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.CreateCommand(new CreateCommandDto { date_command = DateTime.UtcNow, id_carrier = 999 }));
            Assert.Equal(0, await context.Commands.CountAsync());
        }

        // --- UpdateCommand ---

        [Fact]
        public async Task UpdateCommand_ShouldUpdateAllProvidedFields()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var carrier = BuildCarrier();
            context.Carriers.Add(carrier);
            var command = BuildCommand("https://old");
            context.Commands.Add(command);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var newDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
            var delivery = new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc);

            var result = await service.UpdateCommand(command.id_command, new UpdateCommandDto
            {
                price_command = 99f,
                url_command = "https://new",
                status_command = CommandStatus.Delivered,
                date_command = newDate,
                date_delivery_command = delivery,
                tracking_number_command = "TRK1",
                id_carrier = carrier.id_carrier,
                is_tracking_requested = true
            });

            Assert.Equal(99f, result.price_command);
            Assert.Equal("https://new", result.url_command);
            Assert.Equal(CommandStatus.Delivered, result.status_command);
            Assert.Equal(newDate, result.date_command);
            Assert.Equal(delivery, result.date_delivery_command);
            Assert.Equal("TRK1", result.tracking_number_command);
            Assert.Equal(carrier.id_carrier, result.id_carrier);
            Assert.True(result.is_tracking_requested);
        }

        [Fact]
        public async Task UpdateCommand_ShouldKeepValues_WhenDtoIsEmpty()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var command = BuildCommand("https://keep");
            context.Commands.Add(command);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateCommand(command.id_command, new UpdateCommandDto());

            Assert.Equal("https://keep", result.url_command);
        }

        [Fact]
        public async Task UpdateCommand_ShouldThrowKeyNotFoundException_WhenCarrierDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var command = BuildCommand();
            context.Commands.Add(command);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.UpdateCommand(command.id_command, new UpdateCommandDto { id_carrier = 999 }));
        }

        [Fact]
        public async Task UpdateCommand_ShouldThrowKeyNotFoundException_WhenCommandDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateCommand(999, new UpdateCommandDto()));
        }

        // --- DeleteCommand ---

        [Fact]
        public async Task DeleteCommand_ShouldRemoveCommand_AndDeleteDirectory()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var command = BuildCommand();
            context.Commands.Add(command);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.DeleteCommand(command.id_command);

            Assert.Equal(0, await context.Commands.CountAsync());
            _fileService.Verify(f => f.DeleteDirectory(Path.Combine("commandDocuments", command.id_command.ToString())), Times.Once);
        }

        [Fact]
        public async Task DeleteCommand_ShouldThrowKeyNotFoundException_WhenCommandDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteCommand(999));
            _fileService.Verify(f => f.DeleteDirectory(It.IsAny<string>()), Times.Never);
        }

        // --- UpdateCommandStatusByTracking ---

        private static async Task<Commands> SeedTrackedCommand(ApplicationDbContext context, int carrierKey = 5, string tracking = "TRK123",
            bool validated = false, bool active = true)
        {
            var carrier = BuildCarrier(carrierKey);
            context.Carriers.Add(carrier);
            await context.SaveChangesAsync();
            var command = BuildCommand();
            command.id_carrier = carrier.id_carrier;
            command.tracking_number_command = tracking;
            command.is_tracking_validated = validated;
            command.is_active = active;
            context.Commands.Add(command);
            await context.SaveChangesAsync();
            return command;
        }

        [Fact]
        public async Task UpdateCommandStatusByTracking_Register_ShouldValidateAndActivate()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var command = await SeedTrackedCommand(context, validated: false, active: false);
            var service = CreateService(context);

            await service.UpdateCommandStatusByTracking("TRK123", 5, "register");

            var reloaded = await context.Commands.FindAsync(command.id_command);
            Assert.True(reloaded!.is_tracking_validated);
            Assert.True(reloaded.is_active);
        }

        [Fact]
        public async Task UpdateCommandStatusByTracking_StopTrack_ShouldDeactivate()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var command = await SeedTrackedCommand(context, validated: true, active: true);
            var service = CreateService(context);

            await service.UpdateCommandStatusByTracking("TRK123", 5, "stoptrack");

            var reloaded = await context.Commands.FindAsync(command.id_command);
            Assert.True(reloaded!.is_tracking_validated);
            Assert.False(reloaded.is_active);
        }

        [Fact]
        public async Task UpdateCommandStatusByTracking_Retrack_ShouldReactivate()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var command = await SeedTrackedCommand(context, validated: true, active: false);
            var service = CreateService(context);

            await service.UpdateCommandStatusByTracking("TRK123", 5, "retrack");

            var reloaded = await context.Commands.FindAsync(command.id_command);
            Assert.True(reloaded!.is_active);
        }

        [Fact]
        public async Task UpdateCommandStatusByTracking_DeleteTrack_ShouldInvalidateAndDeactivate()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var command = await SeedTrackedCommand(context, validated: true, active: true);
            var service = CreateService(context);

            await service.UpdateCommandStatusByTracking("TRK123", 5, "deletetrack");

            var reloaded = await context.Commands.FindAsync(command.id_command);
            Assert.False(reloaded!.is_tracking_validated);
            Assert.False(reloaded.is_active);
        }

        [Fact]
        public async Task UpdateCommandStatusByTracking_UnknownAction_ShouldLeaveCommandUntouched()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var command = await SeedTrackedCommand(context, validated: true, active: true);
            var service = CreateService(context);

            await service.UpdateCommandStatusByTracking("TRK123", 5, "unknown");

            var reloaded = await context.Commands.FindAsync(command.id_command);
            Assert.True(reloaded!.is_tracking_validated);
            Assert.True(reloaded.is_active);
        }

        [Theory]
        [InlineData("OTHER", 5)]
        [InlineData("TRK123", 6)]
        public async Task UpdateCommandStatusByTracking_ShouldIgnoreNonMatchingTrackingOrCarrier(string tracking, int carrierKey)
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var command = await SeedTrackedCommand(context, validated: false, active: false);
            var service = CreateService(context);

            await service.UpdateCommandStatusByTracking(tracking, carrierKey, "register");

            var reloaded = await context.Commands.FindAsync(command.id_command);
            Assert.False(reloaded!.is_tracking_validated);
            Assert.False(reloaded.is_active);
        }

        [Fact]
        public async Task UpdateCommandStatusByTracking_ShouldUpdateEveryMatchingCommand()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var first = await SeedTrackedCommand(context, carrierKey: 5, tracking: "TRK123");
            var second = BuildCommand("https://second");
            second.id_carrier = first.id_carrier;
            second.tracking_number_command = "TRK123";
            context.Commands.Add(second);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.UpdateCommandStatusByTracking("TRK123", 5, "register");

            Assert.All(await context.Commands.ToListAsync(), c => Assert.True(c.is_tracking_validated));
        }
    }
}
