using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using electrostore;
using electrostore.Dto;
using electrostore.Models;
using electrostore.Services.CommandService;
using electrostore.Services.FileService;
using electrostore.Tests.Utils;

namespace electrostore.Tests.Services
{
    public class CommandServiceTests : TestBase
    {
        private readonly Mock<IFileService> _mockFileService;

        public CommandServiceTests()
        {
            _mockFileService = new Mock<IFileService>();
            _mockFileService.Setup(fs => fs.CreateDirectory(It.IsAny<string>())).Returns(Task.CompletedTask);
            _mockFileService.Setup(fs => fs.DeleteDirectory(It.IsAny<string>())).Returns(Task.CompletedTask);
        }

        [Fact]
        public async Task GetCommands_ShouldReturnCommands_WhenCommandsExist()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);

            for (int i = 1; i <= 5; i++)
            {
                context.Commands.Add(new Commands
                {
                    id_command = i,
                    prix_command = 10 + i,
                    url_command = $"https://example.com/c/{i}",
                    status_command = "pending",
                    date_command = new DateTime(2024, 1, i)
                });
            }
            await context.SaveChangesAsync();

            var service = new CommandService(_mapper, context, _mockFileService.Object);

            // Act
            var result = await service.GetCommands(5, 0);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Count());
        }

        [Fact]
        public async Task GetCommandsCount_ShouldReturnCorrectCount()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);

            for (int i = 1; i <= 3; i++)
            {
                context.Commands.Add(new Commands
                {
                    id_command = i,
                    prix_command = 10 + i,
                    url_command = $"https://example.com/c/{i}",
                    status_command = "pending",
                    date_command = new DateTime(2024, 1, i)
                });
            }
            await context.SaveChangesAsync();

            var service = new CommandService(_mapper, context, _mockFileService.Object);

            // Act
            var count = await service.GetCommandsCount();

            // Assert
            Assert.Equal(3, count);
        }

        [Fact]
        public async Task GetCommandById_ShouldReturnCommand_WhenCommandExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Commands.Add(new Commands
            {
                id_command = 1,
                prix_command = 25,
                url_command = "https://example.com/c/1",
                status_command = "pending",
                date_command = new DateTime(2024, 1, 1)
            });
            await context.SaveChangesAsync();

            var service = new CommandService(_mapper, context, _mockFileService.Object);

            var expected = new ReadExtendedCommandDto
            {
                id_command = 1,
                prix_command = 25,
                url_command = "https://example.com/c/1",
                status_command = "pending",
                date_command = new DateTime(2024, 1, 1)
            };

            // Act
            var result = await service.GetCommandById(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expected.id_command, result.id_command);
            Assert.Equal(expected.prix_command, result.prix_command);
            Assert.Equal(expected.url_command, result.url_command);
            Assert.Equal(expected.status_command, result.status_command);
            Assert.Equal(expected.date_command, result.date_command);
        }

        [Fact]
        public async Task CreateCommand_ShouldCreateCommand_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = new CommandService(_mapper, context, _mockFileService.Object);

            var createDto = new CreateCommandDto
            {
                prix_command = 30,
                url_command = "https://example.com/c/new",
                status_command = "pending",
                date_command = new DateTime(2024, 2, 1),
                date_livraison_command = null
            };

            // Act
            var result = await service.CreateCommand(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.id_command);
            Assert.Equal(30, result.prix_command);
            Assert.Equal("https://example.com/c/new", result.url_command);
            Assert.Equal("pending", result.status_command);
            Assert.Equal(new DateTime(2024, 2, 1), result.date_command);

            var commandInDb = await context.Commands.FirstOrDefaultAsync(c => c.url_command == "https://example.com/c/new");
            Assert.NotNull(commandInDb);
        }

        [Fact]
        public async Task UpdateCommand_ShouldUpdateCommand_WhenCommandExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Commands.Add(new Commands
            {
                id_command = 1,
                prix_command = 20,
                url_command = "https://example.com/c/1",
                status_command = "pending",
                date_command = new DateTime(2024, 1, 1)
            });
            await context.SaveChangesAsync();

            var service = new CommandService(_mapper, context, _mockFileService.Object);

            var updateDto = new UpdateCommandDto
            {
                prix_command = 99,
                url_command = "https://example.com/c/1-updated",
                status_command = "delivered",
                date_command = new DateTime(2024, 3, 1),
                date_livraison_command = new DateTime(2024, 3, 15)
            };

            // Act
            var result = await service.UpdateCommand(1, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.id_command);
            Assert.Equal(99, result.prix_command);
            Assert.Equal("https://example.com/c/1-updated", result.url_command);
            Assert.Equal("delivered", result.status_command);
            Assert.Equal(new DateTime(2024, 3, 1), result.date_command);
            Assert.Equal(new DateTime(2024, 3, 15), result.date_livraison_command);

            var commandInDb = await context.Commands.FindAsync(1);
            Assert.NotNull(commandInDb);
            Assert.Equal(99, commandInDb.prix_command);
            Assert.Equal("https://example.com/c/1-updated", commandInDb.url_command);
            Assert.Equal("delivered", commandInDb.status_command);
            Assert.Equal(new DateTime(2024, 3, 1), commandInDb.date_command);
            Assert.Equal(new DateTime(2024, 3, 15), commandInDb.date_livraison_command);
        }

        [Fact]
        public async Task DeleteCommand_ShouldDeleteCommand_WhenCommandExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Commands.Add(new Commands
            {
                id_command = 1,
                prix_command = 20,
                url_command = "https://example.com/c/1",
                status_command = "pending",
                date_command = new DateTime(2024, 1, 1)
            });
            await context.SaveChangesAsync();

            var service = new CommandService(_mapper, context, _mockFileService.Object);

            // Act
            await service.DeleteCommand(1);

            // Assert
            var commandInDb = await context.Commands.FindAsync(1);
            Assert.Null(commandInDb);
        }
    }
}