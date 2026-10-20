using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
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
        private readonly Mock<IFileService> _fileService = new();

        private CommandDocumentService CreateService(ApplicationDbContext context) =>
            new(_mapper, context, _fileService.Object);

        private static Commands BuildCommand(string url = "https://example.com") => new()
        {
            url_command = url,
            date_command = DateTime.UtcNow
        };

        private static Mock<IFormFile> BuildFormFile(string fileName = "doc.pdf", string contentType = "application/pdf", long length = 1024)
        {
            var file = new Mock<IFormFile>();
            file.Setup(f => f.FileName).Returns(fileName);
            file.Setup(f => f.ContentType).Returns(contentType);
            file.Setup(f => f.Length).Returns(length);
            return file;
        }

        // --- GetCommandsDocumentsByCommandId ---

        [Fact]
        public async Task GetCommandsDocumentsByCommandId_ShouldReturnDocumentsForCommand()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var command = BuildCommand();
            context.Commands.Add(command);
            await context.SaveChangesAsync();
            context.CommandsDocuments.Add(new CommandsDocuments
            {
                id_command = command.id_command,
                url_command_document = "path/doc.pdf",
                name_command_document = "doc",
                type_command_document = "application/pdf",
                size_command_document = 100
            });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetCommandsDocumentsByCommandId(command.id_command);

            Assert.Single(result.data);
        }

        [Fact]
        public async Task GetCommandsDocumentsByCommandId_ShouldThrowKeyNotFoundException_WhenCommandDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetCommandsDocumentsByCommandId(999));
        }

        // --- GetCommandDocumentById ---

        [Fact]
        public async Task GetCommandDocumentById_ShouldReturnDocument_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var command = BuildCommand();
            context.Commands.Add(command);
            await context.SaveChangesAsync();
            var document = new CommandsDocuments
            {
                id_command = command.id_command,
                url_command_document = "path/doc.pdf",
                name_command_document = "doc",
                type_command_document = "application/pdf",
                size_command_document = 100
            };
            context.CommandsDocuments.Add(document);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetCommandDocumentById(document.id_command_document);

            Assert.Equal(document.id_command_document, result.id_command_document);
        }

        [Fact]
        public async Task GetCommandDocumentById_ShouldThrowKeyNotFoundException_WhenCommandIdDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var command = BuildCommand();
            context.Commands.Add(command);
            await context.SaveChangesAsync();
            var document = new CommandsDocuments
            {
                id_command = command.id_command,
                url_command_document = "path/doc.pdf",
                name_command_document = "doc",
                type_command_document = "application/pdf",
                size_command_document = 100
            };
            context.CommandsDocuments.Add(document);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetCommandDocumentById(document.id_command_document, command.id_command + 1));
        }

        [Fact]
        public async Task GetCommandDocumentById_ShouldThrowKeyNotFoundException_WhenDocumentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetCommandDocumentById(999));
        }

        // --- CreateCommandDocument ---

        [Fact]
        public async Task CreateCommandDocument_ShouldPersistDocument_WhenCommandExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var command = BuildCommand();
            context.Commands.Add(command);
            await context.SaveChangesAsync();
            _fileService.Setup(f => f.SaveFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<bool>()))
                .ReturnsAsync(new SaveFileResult { path = "commandDocuments/1/doc.pdf", mime_type = "application/pdf" });
            var service = CreateService(context);
            var dto = new CreateCommandDocumentDto { id_command = command.id_command, name_command_document = "doc", document = BuildFormFile().Object };

            var result = await service.CreateCommandDocument(dto);

            Assert.Equal("doc", result.name_command_document);
            Assert.Equal("commandDocuments/1/doc.pdf", result.url_command_document);
            Assert.Equal(1, await context.CommandsDocuments.CountAsync());
        }

        [Fact]
        public async Task CreateCommandDocument_ShouldThrowKeyNotFoundException_WhenCommandDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateCommandDocumentDto { id_command = 999, name_command_document = "doc", document = BuildFormFile().Object };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateCommandDocument(dto));
        }

        // --- UpdateCommandDocument ---

        [Fact]
        public async Task UpdateCommandDocument_ShouldUpdateName()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var command = BuildCommand();
            context.Commands.Add(command);
            await context.SaveChangesAsync();
            var document = new CommandsDocuments
            {
                id_command = command.id_command,
                url_command_document = "path/doc.pdf",
                name_command_document = "doc",
                type_command_document = "application/pdf",
                size_command_document = 100
            };
            context.CommandsDocuments.Add(document);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new UpdateCommandDocumentDto { name_command_document = "renamed" };

            var result = await service.UpdateCommandDocument(document.id_command_document, dto);

            Assert.Equal("renamed", result.name_command_document);
        }

        [Fact]
        public async Task UpdateCommandDocument_ShouldThrowKeyNotFoundException_WhenDocumentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateCommandDocument(999, new UpdateCommandDocumentDto()));
        }

        // --- DeleteCommandDocument ---

        [Fact]
        public async Task DeleteCommandDocument_ShouldRemoveDocument_AndDeleteFile()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var command = BuildCommand();
            context.Commands.Add(command);
            await context.SaveChangesAsync();
            var document = new CommandsDocuments
            {
                id_command = command.id_command,
                url_command_document = "path/doc.pdf",
                name_command_document = "doc",
                type_command_document = "application/pdf",
                size_command_document = 100
            };
            context.CommandsDocuments.Add(document);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.DeleteCommandDocument(document.id_command_document);

            Assert.Equal(0, await context.CommandsDocuments.CountAsync());
            _fileService.Verify(f => f.DeleteFile("path/doc.pdf"), Times.Once);
        }

        [Fact]
        public async Task DeleteCommandDocument_ShouldThrowKeyNotFoundException_WhenDocumentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteCommandDocument(999));
        }
    }
}
