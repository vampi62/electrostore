using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.CommandDocumentService;
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class CommandDocumentServiceTests : TestBase
    {
        private readonly Mock<IFileService> _fileService;

        public CommandDocumentServiceTests()
        {
            _fileService = new Mock<IFileService>();
        }

        private CommandDocumentService CreateService(ApplicationDbContext context)
        {
            return new CommandDocumentService(_mapper, context, _fileService.Object);
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

        private static CommandsDocuments BuildCommandDocument(int id, int commandId, string name = "Document")
        {
            return new CommandsDocuments
            {
                id_command_document = id,
                id_command = commandId,
                url_command_document = "commandDocuments/" + commandId + "/" + id + ".pdf",
                name_command_document = name,
                type_command_document = "application/pdf",
                size_command_document = 1024
            };
        }

        private static Mock<IFormFile> BuildFormFile(string fileName = "doc.pdf", string contentType = "application/pdf", long length = 1024)
        {
            var file = new Mock<IFormFile>();
            file.Setup(f => f.FileName).Returns(fileName);
            file.Setup(f => f.ContentType).Returns(contentType);
            file.Setup(f => f.Length).Returns(length);
            file.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
            return file;
        }

        // --- GetCommandsDocumentsByCommandId ---

        [Fact]
        public async Task GetCommandsDocumentsByCommandId_ShouldThrowKeyNotFoundException_WhenCommandNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetCommandsDocumentsByCommandId(1);
            });
        }

        [Fact]
        public async Task GetCommandsDocumentsByCommandId_ShouldReturnOnlyDocumentsForGivenCommand()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Commands.Add(BuildCommand(2));
            context.CommandsDocuments.Add(BuildCommandDocument(1, 1));
            context.CommandsDocuments.Add(BuildCommandDocument(2, 1));
            context.CommandsDocuments.Add(BuildCommandDocument(3, 2));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetCommandsDocumentsByCommandId(1);
            // Assert
            Assert.Equal(2, result.data.Count());
            Assert.All(result.data, d => Assert.Equal(1, d.id_command));
            Assert.Equal(2, result.pagination.total);
        }

        // --- GetCommandDocumentById ---

        [Fact]
        public async Task GetCommandDocumentById_ShouldReturnDocument_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.CommandsDocuments.Add(BuildCommandDocument(1, 1, "Invoice"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetCommandDocumentById(1);
            // Assert
            Assert.Equal("Invoice", result.name_command_document);
        }

        [Fact]
        public async Task GetCommandDocumentById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetCommandDocumentById(999);
            });
        }

        [Fact]
        public async Task GetCommandDocumentById_ShouldThrowKeyNotFoundException_WhenCommandIdDoesNotMatch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Commands.Add(BuildCommand(2));
            context.CommandsDocuments.Add(BuildCommandDocument(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetCommandDocumentById(1, commandId: 2);
            });
        }

        // --- CreateCommandDocument ---

        [Fact]
        public async Task CreateCommandDocument_ShouldThrowKeyNotFoundException_WhenCommandNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateCommandDocumentDto { id_command = 999, name_command_document = "Invoice", document = BuildFormFile().Object };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreateCommandDocument(dto);
            });
        }

        [Fact]
        public async Task CreateCommandDocument_ShouldCreateDocument_WhenCommandExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            await context.SaveChangesAsync();
            _fileService.Setup(f => f.SaveFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<bool>()))
                .ReturnsAsync(new SaveFileResult { path = "commandDocuments/1/doc.pdf", mimeType = "application/pdf" });
            var service = CreateService(context);
            var dto = new CreateCommandDocumentDto { id_command = 1, name_command_document = "Invoice", document = BuildFormFile().Object };
            // Act
            var result = await service.CreateCommandDocument(dto);
            // Assert
            Assert.Equal(1, result.id_command);
            Assert.Equal("Invoice", result.name_command_document);
            Assert.Equal("commandDocuments/1/doc.pdf", result.url_command_document);
            Assert.Equal(1, await context.CommandsDocuments.CountAsync());
        }

        // --- UpdateCommandDocument ---

        [Fact]
        public async Task UpdateCommandDocument_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateCommandDocument(999, new UpdateCommandDocumentDto());
            });
        }

        [Fact]
        public async Task UpdateCommandDocument_ShouldThrowKeyNotFoundException_WhenCommandIdDoesNotMatch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Commands.Add(BuildCommand(2));
            context.CommandsDocuments.Add(BuildCommandDocument(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateCommandDocument(1, new UpdateCommandDocumentDto(), commandId: 2);
            });
        }

        [Fact]
        public async Task UpdateCommandDocument_ShouldUpdateName_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.CommandsDocuments.Add(BuildCommandDocument(1, 1, "Old name"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.UpdateCommandDocument(1, new UpdateCommandDocumentDto { name_command_document = "New name" });
            // Assert
            Assert.Equal("New name", result.name_command_document);
        }

        // --- DeleteCommandDocument ---

        [Fact]
        public async Task DeleteCommandDocument_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeleteCommandDocument(999);
            });
        }

        [Fact]
        public async Task DeleteCommandDocument_ShouldThrowKeyNotFoundException_WhenCommandIdDoesNotMatch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Commands.Add(BuildCommand(2));
            context.CommandsDocuments.Add(BuildCommandDocument(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeleteCommandDocument(1, commandId: 2);
            });
        }

        [Fact]
        public async Task DeleteCommandDocument_ShouldDeleteDocumentAndFile_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.CommandsDocuments.Add(BuildCommandDocument(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            await service.DeleteCommandDocument(1);
            // Assert
            Assert.Equal(0, await context.CommandsDocuments.CountAsync());
            _fileService.Verify(f => f.DeleteFile(It.IsAny<string>()), Times.Once);
        }
    }
}
