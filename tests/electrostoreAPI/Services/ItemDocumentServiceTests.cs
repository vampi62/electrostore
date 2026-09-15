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
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Services.ItemDocumentService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class ItemDocumentServiceTests : TestBase
    {
        private readonly Mock<IFileService> _fileService = new();

        private ItemDocumentService CreateService(ApplicationDbContext context) =>
            new(_mapper, context, _fileService.Object);

        private static Items BuildItem(string reference = "item") => new()
        {
            reference_name_item = reference,
            friendly_name_item = reference,
            threshold_min_item = 0
        };

        private static Mock<IFormFile> BuildFormFile(string fileName = "doc.pdf", string contentType = "application/pdf", long length = 1024)
        {
            var file = new Mock<IFormFile>();
            file.Setup(f => f.FileName).Returns(fileName);
            file.Setup(f => f.ContentType).Returns(contentType);
            file.Setup(f => f.Length).Returns(length);
            return file;
        }

        // --- GetItemsDocumentsByItemId ---

        [Fact]
        public async Task GetItemsDocumentsByItemId_ShouldReturnDocumentsForItem()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem();
            context.Items.Add(item);
            await context.SaveChangesAsync();
            context.ItemsDocuments.Add(new ItemsDocuments
            {
                id_item = item.id_item,
                url_item_document = "path/doc.pdf",
                name_item_document = "doc",
                type_item_document = "application/pdf",
                size_item_document = 100
            });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetItemsDocumentsByItemId(item.id_item);

            Assert.Single(result.data);
        }

        [Fact]
        public async Task GetItemsDocumentsByItemId_ShouldThrowKeyNotFoundException_WhenItemDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetItemsDocumentsByItemId(999));
        }

        // --- GetItemDocumentById ---

        [Fact]
        public async Task GetItemDocumentById_ShouldReturnDocument_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem();
            context.Items.Add(item);
            await context.SaveChangesAsync();
            var document = new ItemsDocuments
            {
                id_item = item.id_item,
                url_item_document = "path/doc.pdf",
                name_item_document = "doc",
                type_item_document = "application/pdf",
                size_item_document = 100
            };
            context.ItemsDocuments.Add(document);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetItemDocumentById(document.id_item_document);

            Assert.Equal(document.id_item_document, result.id_item_document);
        }

        [Fact]
        public async Task GetItemDocumentById_ShouldThrowKeyNotFoundException_WhenItemIdDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem();
            context.Items.Add(item);
            await context.SaveChangesAsync();
            var document = new ItemsDocuments
            {
                id_item = item.id_item,
                url_item_document = "path/doc.pdf",
                name_item_document = "doc",
                type_item_document = "application/pdf",
                size_item_document = 100
            };
            context.ItemsDocuments.Add(document);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetItemDocumentById(document.id_item_document, item.id_item + 1));
        }

        [Fact]
        public async Task GetItemDocumentById_ShouldThrowKeyNotFoundException_WhenDocumentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetItemDocumentById(999));
        }

        // --- CreateItemDocument ---

        [Fact]
        public async Task CreateItemDocument_ShouldPersistDocument_WhenItemExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem();
            context.Items.Add(item);
            await context.SaveChangesAsync();
            _fileService.Setup(f => f.SaveFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<bool>()))
                .ReturnsAsync(new SaveFileResult { path = "itemDocuments/1/doc.pdf", mime_type = "application/pdf" });
            var service = CreateService(context);
            var dto = new CreateItemDocumentDto { id_item = item.id_item, name_item_document = "doc", document = BuildFormFile().Object };

            var result = await service.CreateItemDocument(dto);

            Assert.Equal("doc", result.name_item_document);
            Assert.Equal("itemDocuments/1/doc.pdf", result.url_item_document);
            Assert.Equal(1, await context.ItemsDocuments.CountAsync());
        }

        [Fact]
        public async Task CreateItemDocument_ShouldThrowKeyNotFoundException_WhenItemDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateItemDocumentDto { id_item = 999, name_item_document = "doc", document = BuildFormFile().Object };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateItemDocument(dto));
        }

        // --- UpdateItemDocument ---

        [Fact]
        public async Task UpdateItemDocument_ShouldUpdateName()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem();
            context.Items.Add(item);
            await context.SaveChangesAsync();
            var document = new ItemsDocuments
            {
                id_item = item.id_item,
                url_item_document = "path/doc.pdf",
                name_item_document = "doc",
                type_item_document = "application/pdf",
                size_item_document = 100
            };
            context.ItemsDocuments.Add(document);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new UpdateItemDocumentDto { name_item_document = "renamed" };

            var result = await service.UpdateItemDocument(document.id_item_document, dto);

            Assert.Equal("renamed", result.name_item_document);
        }

        [Fact]
        public async Task UpdateItemDocument_ShouldThrowKeyNotFoundException_WhenDocumentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateItemDocument(999, new UpdateItemDocumentDto()));
        }

        // --- DeleteItemDocument ---

        [Fact]
        public async Task DeleteItemDocument_ShouldRemoveDocument_AndDeleteFile()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem();
            context.Items.Add(item);
            await context.SaveChangesAsync();
            var document = new ItemsDocuments
            {
                id_item = item.id_item,
                url_item_document = "path/doc.pdf",
                name_item_document = "doc",
                type_item_document = "application/pdf",
                size_item_document = 100
            };
            context.ItemsDocuments.Add(document);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.DeleteItemDocument(document.id_item_document);

            Assert.Equal(0, await context.ItemsDocuments.CountAsync());
            _fileService.Verify(f => f.DeleteFile("path/doc.pdf"), Times.Once);
        }

        [Fact]
        public async Task DeleteItemDocument_ShouldThrowKeyNotFoundException_WhenDocumentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteItemDocument(999));
        }
    }
}
