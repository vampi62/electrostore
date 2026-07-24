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
using ElectrostoreAPI.Services.CommandHistoryService;
using ElectrostoreAPI.Services.CommandService;
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class CommandServiceTests : TestBase
    {
        private readonly Mock<IFileService> _fileService;
        private readonly Mock<IKafkaProducerService> _kafkaProducerService;
        private readonly Mock<ICommandHistoryService> _commandHistoryService;

        public CommandServiceTests()
        {
            _fileService = new Mock<IFileService>();
            _kafkaProducerService = new Mock<IKafkaProducerService>();
            _commandHistoryService = new Mock<ICommandHistoryService>();
        }

        private CommandService CreateService(ApplicationDbContext context)
        {
            return new CommandService(_mapper, context, _fileService.Object, _kafkaProducerService.Object, _commandHistoryService.Object);
        }

        private static Carriers BuildCarrier(int id, int key = 100)
        {
            return new Carriers
            {
                id_carrier = id,
                key = key,
                name = "Carrier"
            };
        }

        private static Commands BuildCommand(int id, int carrierId = 0, string trackingNumber = "", bool isTrackingRequested = false, bool isTrackingValidated = false, bool isActive = true)
        {
            return new Commands
            {
                id_command = id,
                url_command = "https://example.com/order",
                tracking_number = trackingNumber,
                id_carrier = carrierId,
                date_command = DateTime.UtcNow,
                is_tracking_requested = isTrackingRequested,
                is_tracking_validated = isTrackingValidated,
                is_active = isActive
            };
        }

        // --- GetCommands ---

        [Fact]
        public async Task GetCommands_ShouldReturnAll_Paginated()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Commands.Add(BuildCommand(2));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetCommands();
            // Assert
            Assert.Equal(2, result.data.Count());
            Assert.Equal(2, result.pagination.total);
        }

        [Fact]
        public async Task GetCommands_ShouldFilterByIdResearch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Commands.Add(BuildCommand(2));
            context.Commands.Add(BuildCommand(3));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetCommands(idResearch: new List<int> { 2 });
            // Assert
            var command = Assert.Single(result.data);
            Assert.Equal(2, command.id_command);
        }

        // --- GetCommandById ---

        [Fact]
        public async Task GetCommandById_ShouldReturnCommand_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1, trackingNumber: "TRACK-1"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetCommandById(1);
            // Assert
            Assert.Equal("TRACK-1", result.tracking_number);
        }

        [Fact]
        public async Task GetCommandById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetCommandById(999);
            });
        }

        // --- CreateCommand ---

        [Fact]
        public async Task CreateCommand_ShouldThrowKeyNotFoundException_WhenCarrierNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateCommandDto { status_command = CommandStatus.Created, date_command = DateTime.UtcNow, id_carrier = 999 };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreateCommand(dto);
            });
        }

        [Fact]
        public async Task CreateCommand_ShouldCreateCommand_WithoutPublishing_WhenTrackingNotRequested()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateCommandDto { status_command = CommandStatus.Created, date_command = DateTime.UtcNow };
            // Act
            var result = await service.CreateCommand(dto);
            // Assert
            Assert.Equal(1, await context.Commands.CountAsync());
            _kafkaProducerService.Verify(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
            _fileService.Verify(f => f.CreateDirectory(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task CreateCommand_ShouldPublishTrackingRequestAdd_WhenTrackingRequestedWithCarrierAndTrackingNumber()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Carriers.Add(BuildCarrier(1, key: 100));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateCommandDto
            {
                status_command = CommandStatus.Created,
                date_command = DateTime.UtcNow,
                id_carrier = 1,
                tracking_number = "TRACK-1",
                is_tracking_requested = true
            };
            // Act
            var result = await service.CreateCommand(dto);
            // Assert
            Assert.Equal("TRACK-1", result.tracking_number);
            _kafkaProducerService.Verify(k => k.PublishAsync("tracking-request-add", "TRACK-1_100", It.IsAny<string>(), default), Times.Once);
        }

        // --- UpdateCommand ---

        [Fact]
        public async Task UpdateCommand_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateCommand(999, new UpdateCommandDto());
            });
        }

        [Fact]
        public async Task UpdateCommand_ShouldThrowKeyNotFoundException_WhenCarrierNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateCommand(1, new UpdateCommandDto { id_carrier = 999 });
            });
        }

        [Fact]
        public async Task UpdateCommand_ShouldUpdateFields_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.UpdateCommand(1, new UpdateCommandDto { prix_command = 42f, status_command = CommandStatus.Processing });
            // Assert
            Assert.Equal(42f, result.prix_command);
            Assert.Equal(CommandStatus.Processing, result.status_command);
        }

        [Fact]
        public async Task UpdateCommand_ShouldPublishTrackingRequestAdd_WhenTrackingRequestedBecomesTrue()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Carriers.Add(BuildCarrier(1, key: 100));
            context.Commands.Add(BuildCommand(1, carrierId: 1, trackingNumber: "TRACK-1", isTrackingRequested: false, isTrackingValidated: false));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            await service.UpdateCommand(1, new UpdateCommandDto { is_tracking_requested = true });
            // Assert
            _kafkaProducerService.Verify(k => k.PublishAsync("tracking-request-add", "TRACK-1_100", It.IsAny<string>(), default), Times.Once);
        }

        [Fact]
        public async Task UpdateCommand_ShouldPublishTrackingRequestChange_WhenCarrierChangedWhileValidated()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Carriers.Add(BuildCarrier(1, key: 100));
            context.Carriers.Add(BuildCarrier(2, key: 200));
            context.Commands.Add(BuildCommand(1, carrierId: 1, trackingNumber: "TRACK-1", isTrackingValidated: true));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            await service.UpdateCommand(1, new UpdateCommandDto { id_carrier = 2 });
            // Assert
            _kafkaProducerService.Verify(k => k.PublishAsync("tracking-request-change", "TRACK-1_200", It.IsAny<string>(), default), Times.Once);
        }

        [Fact]
        public async Task UpdateCommand_ShouldPublishTrackingRequestStop_WhenTrackingRequestedBecomesFalseWhileValidatedAndActive()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Carriers.Add(BuildCarrier(1, key: 100));
            context.Commands.Add(BuildCommand(1, carrierId: 1, trackingNumber: "TRACK-1", isTrackingRequested: true, isTrackingValidated: true, isActive: true));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            await service.UpdateCommand(1, new UpdateCommandDto { is_tracking_requested = false });
            // Assert
            _kafkaProducerService.Verify(k => k.PublishAsync("tracking-request-stop", "TRACK-1_100", It.IsAny<string>(), default), Times.Once);
        }

        [Fact]
        public async Task UpdateCommand_ShouldPublishTrackingRequestResume_WhenTrackingRequestedBecomesTrueWhileValidatedAndInactive()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Carriers.Add(BuildCarrier(1, key: 100));
            context.Commands.Add(BuildCommand(1, carrierId: 1, trackingNumber: "TRACK-1", isTrackingRequested: false, isTrackingValidated: true, isActive: false));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            await service.UpdateCommand(1, new UpdateCommandDto { is_tracking_requested = true });
            // Assert
            _kafkaProducerService.Verify(k => k.PublishAsync("tracking-request-resume", "TRACK-1_100", It.IsAny<string>(), default), Times.Once);
        }

        [Fact]
        public async Task UpdateCommand_ShouldPublishTrackingRequestDelete_WhenTrackingNumberChangedWhileValidated()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Carriers.Add(BuildCarrier(1, key: 100));
            context.Commands.Add(BuildCommand(1, carrierId: 1, trackingNumber: "TRACK-OLD", isTrackingValidated: true));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            await service.UpdateCommand(1, new UpdateCommandDto { tracking_number = "TRACK-NEW" });
            // Assert
            _kafkaProducerService.Verify(k => k.PublishAsync("tracking-request-delete", "TRACK-NEW_100", It.IsAny<string>(), default), Times.Once);
        }

        // --- DeleteCommand ---

        [Fact]
        public async Task DeleteCommand_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeleteCommand(999);
            });
        }

        [Fact]
        public async Task DeleteCommand_ShouldDeleteAndPublishTrackingRequestDelete_WhenActiveAndValidated()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Carriers.Add(BuildCarrier(1, key: 100));
            context.Commands.Add(BuildCommand(1, carrierId: 1, trackingNumber: "TRACK-1", isTrackingValidated: true, isActive: true));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            await service.DeleteCommand(1);
            // Assert
            Assert.Equal(0, await context.Commands.CountAsync());
            _kafkaProducerService.Verify(k => k.PublishAsync("tracking-request-delete", "TRACK-1_100", It.IsAny<string>(), default), Times.Once);
            _fileService.Verify(f => f.DeleteDirectory(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task DeleteCommand_ShouldDeleteWithoutPublishing_WhenNotValidated()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1, isTrackingValidated: false));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            await service.DeleteCommand(1);
            // Assert
            Assert.Equal(0, await context.Commands.CountAsync());
            _kafkaProducerService.Verify(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
        }

        // --- UpdateCommandStatusByTracking ---

        [Fact]
        public async Task UpdateCommandStatusByTracking_ShouldDoNothing_WhenNoMatchingCommands()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act
            var exception = await Record.ExceptionAsync(async () =>
            {
                await service.UpdateCommandStatusByTracking("TRACK-1", 100, "register");
            });
            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public async Task UpdateCommandStatusByTracking_ShouldSetValidatedAndActive_ForRegisterAction()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Carriers.Add(BuildCarrier(1, key: 100));
            context.Commands.Add(BuildCommand(1, carrierId: 1, trackingNumber: "TRACK-1", isTrackingValidated: false, isActive: false));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            await service.UpdateCommandStatusByTracking("TRACK-1", 100, "register");
            // Assert
            var command = await context.Commands.FindAsync(1);
            Assert.True(command!.is_tracking_validated);
            Assert.True(command.is_active);
        }

        [Fact]
        public async Task UpdateCommandStatusByTracking_ShouldSetInactive_ForStoptrackAction()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Carriers.Add(BuildCarrier(1, key: 100));
            context.Commands.Add(BuildCommand(1, carrierId: 1, trackingNumber: "TRACK-1", isTrackingValidated: true, isActive: true));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            await service.UpdateCommandStatusByTracking("TRACK-1", 100, "stoptrack");
            // Assert
            var command = await context.Commands.FindAsync(1);
            Assert.False(command!.is_active);
        }

        [Fact]
        public async Task UpdateCommandStatusByTracking_ShouldClearValidatedAndActive_ForDeletetrackAction()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Carriers.Add(BuildCarrier(1, key: 100));
            context.Commands.Add(BuildCommand(1, carrierId: 1, trackingNumber: "TRACK-1", isTrackingValidated: true, isActive: true));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            await service.UpdateCommandStatusByTracking("TRACK-1", 100, "deletetrack");
            // Assert
            var command = await context.Commands.FindAsync(1);
            Assert.False(command!.is_tracking_validated);
            Assert.False(command.is_active);
        }
    }
}
