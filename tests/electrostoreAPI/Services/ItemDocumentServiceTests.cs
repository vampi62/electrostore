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
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Services.ItemDocumentService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class ItemDocumentServiceTests : TestBase
    {
        private readonly Mock<IFileService> _fileService;

        public ItemDocumentServiceTests()
        {
            _fileService = new Mock<IFileService>();
        }

        private ItemDocumentService CreateService(ApplicationDbContext context)
        {
            return new ItemDocumentService(_mapper, context, _fileService.Object);
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

        private static ItemsDocuments BuildItemDocument(int id, int itemId, string name = "Document")
        {
            return new ItemsDocuments
            {
                id_item_document = id,
                id_item = itemId,
                url_item_document = "itemDocuments/" + itemId + "/" + id + ".pdf",
                name_item_document = name,
                type_item_document = "application/pdf",
                size_item_document = 1024
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

        // --- GetItemsDocumentsByItemId ---

        [Fact]
        public async Task GetItemsDocumentsByItemId_ShouldThrowKeyNotFoundException_WhenItemNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetItemsDocumentsByItemId(1);
            });
        }

        [Fact]
        public async Task GetItemsDocumentsByItemId_ShouldReturnOnlyDocumentsForGivenItem()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.Items.Add(BuildItem(2));
            context.ItemsDocuments.Add(BuildItemDocument(1, 1));
            context.ItemsDocuments.Add(BuildItemDocument(2, 1));
            context.ItemsDocuments.Add(BuildItemDocument(3, 2));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetItemsDocumentsByItemId(1);
            // Assert
            Assert.Equal(2, result.data.Count());
            Assert.All(result.data, d => Assert.Equal(1, d.id_item));
            Assert.Equal(2, result.pagination.total);
        }

        // --- GetItemDocumentById ---

        [Fact]
        public async Task GetItemDocumentById_ShouldReturnDocument_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.ItemsDocuments.Add(BuildItemDocument(1, 1, "Datasheet"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetItemDocumentById(1);
            // Assert
            Assert.Equal("Datasheet", result.name_item_document);
        }

        [Fact]
        public async Task GetItemDocumentById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetItemDocumentById(999);
            });
        }

        [Fact]
        public async Task GetItemDocumentById_ShouldThrowKeyNotFoundException_WhenItemIdDoesNotMatch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.Items.Add(BuildItem(2));
            context.ItemsDocuments.Add(BuildItemDocument(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetItemDocumentById(1, itemId: 2);
            });
        }

        // --- CreateItemDocument ---

        [Fact]
        public async Task CreateItemDocument_ShouldThrowKeyNotFoundException_WhenItemNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateItemDocumentDto { id_item = 999, name_item_document = "Datasheet", document = BuildFormFile().Object };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreateItemDocument(dto);
            });
        }

        [Fact]
        public async Task CreateItemDocument_ShouldCreateDocument_WhenItemExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            await context.SaveChangesAsync();
            _fileService.Setup(f => f.SaveFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<bool>()))
                .ReturnsAsync(new SaveFileResult { path = "itemDocuments/1/doc.pdf", mimeType = "application/pdf" });
            var service = CreateService(context);
            var dto = new CreateItemDocumentDto { id_item = 1, name_item_document = "Datasheet", document = BuildFormFile().Object };
            // Act
            var result = await service.CreateItemDocument(dto);
            // Assert
            Assert.Equal(1, result.id_item);
            Assert.Equal("Datasheet", result.name_item_document);
            Assert.Equal("itemDocuments/1/doc.pdf", result.url_item_document);
            Assert.Equal(1, await context.ItemsDocuments.CountAsync());
        }

        // --- UpdateItemDocument ---

        [Fact]
        public async Task UpdateItemDocument_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateItemDocument(999, new UpdateItemDocumentDto());
            });
        }

        [Fact]
        public async Task UpdateItemDocument_ShouldThrowKeyNotFoundException_WhenItemIdDoesNotMatch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.Items.Add(BuildItem(2));
            context.ItemsDocuments.Add(BuildItemDocument(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateItemDocument(1, new UpdateItemDocumentDto(), itemId: 2);
            });
        }

        [Fact]
        public async Task UpdateItemDocument_ShouldUpdateName_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.ItemsDocuments.Add(BuildItemDocument(1, 1, "Old name"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.UpdateItemDocument(1, new UpdateItemDocumentDto { name_item_document = "New name" });
            // Assert
            Assert.Equal("New name", result.name_item_document);
        }

        // --- DeleteItemDocument ---

        [Fact]
        public async Task DeleteItemDocument_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeleteItemDocument(999);
            });
        }

        [Fact]
        public async Task DeleteItemDocument_ShouldThrowKeyNotFoundException_WhenItemIdDoesNotMatch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.Items.Add(BuildItem(2));
            context.ItemsDocuments.Add(BuildItemDocument(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeleteItemDocument(1, itemId: 2);
            });
        }

        [Fact]
        public async Task DeleteItemDocument_ShouldDeleteDocumentAndFile_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.ItemsDocuments.Add(BuildItemDocument(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            await service.DeleteItemDocument(1);
            // Assert
            Assert.Equal(0, await context.ItemsDocuments.CountAsync());
            _fileService.Verify(f => f.DeleteFile(It.IsAny<string>()), Times.Once);
        }
    }
}
