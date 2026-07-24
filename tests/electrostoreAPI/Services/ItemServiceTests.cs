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
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Services.ItemHistoryService;
using ElectrostoreAPI.Services.ItemService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class ItemServiceTests : TestBase
    {
        private readonly Mock<IFileService> _fileService;
        private readonly Mock<IItemHistoryService> _itemHistoryService;

        public ItemServiceTests()
        {
            _fileService = new Mock<IFileService>();
            _itemHistoryService = new Mock<IItemHistoryService>();
        }

        private ItemService CreateService(ApplicationDbContext context)
        {
            return new ItemService(_mapper, context, _fileService.Object, _itemHistoryService.Object);
        }

        private static Items BuildItem(int id, string referenceName, string friendlyName = "Item")
        {
            return new Items
            {
                id_item = id,
                reference_name_item = referenceName,
                friendly_name_item = friendlyName
            };
        }

        private static Imgs BuildImg(int id, int itemId)
        {
            return new Imgs
            {
                id_img = id,
                id_item = itemId,
                nom_img = "Image",
                url_picture_img = "images/" + itemId + "/" + id + ".png",
                url_thumbnail_img = "imagesThumbnails/" + itemId + "/" + id + ".png"
            };
        }

        // --- GetItems ---

        [Fact]
        public async Task GetItems_ShouldReturnAll_Paginated()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1, "REF-1"));
            context.Items.Add(BuildItem(2, "REF-2"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetItems();
            // Assert
            Assert.Equal(2, result.data.Count());
            Assert.Equal(2, result.pagination.total);
        }

        [Fact]
        public async Task GetItems_ShouldFilterByIdResearch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1, "REF-1"));
            context.Items.Add(BuildItem(2, "REF-2"));
            context.Items.Add(BuildItem(3, "REF-3"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetItems(idResearch: new List<int> { 2 });
            // Assert
            var item = Assert.Single(result.data);
            Assert.Equal(2, item.id_item);
        }

        [Fact]
        public async Task GetItems_ShouldComputeQuantityFromBoxes()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(new Stores { id_store = 1, nom_store = "Store", mqtt_name_store = "mqtt-1" });
            context.Boxs.Add(new Boxs { id_box = 1, id_store = 1, xend_box = 10, yend_box = 10 });
            context.Items.Add(BuildItem(1, "REF-1"));
            context.ItemsBoxs.Add(new ItemsBoxs { id_item = 1, id_box = 1, qte_item_box = 7 });
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetItems();
            // Assert
            var item = Assert.Single(result.data);
            Assert.Equal(7, item.quantity_item);
        }

        // --- GetItemById ---

        [Fact]
        public async Task GetItemById_ShouldReturnItem_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1, "REF-1", "My Item"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetItemById(1);
            // Assert
            Assert.Equal("My Item", result.friendly_name_item);
        }

        [Fact]
        public async Task GetItemById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetItemById(999);
            });
        }

        // --- CreateItem ---

        [Fact]
        public async Task CreateItem_ShouldThrowKeyNotFoundException_WhenImgNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateItemDto { reference_name_item = "REF-1", friendly_name_item = "Item", seuil_min_item = 0, id_img = 999 };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreateItem(dto);
            });
        }

        [Fact]
        public async Task CreateItem_ShouldThrowInvalidOperationException_WhenReferenceNameAlreadyExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1, "REF-1"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateItemDto { reference_name_item = "REF-1", friendly_name_item = "Item", seuil_min_item = 0 };
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.CreateItem(dto);
            });
        }

        [Fact]
        public async Task CreateItem_ShouldCreateItemDirectoriesAndLogHistory_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateItemDto { reference_name_item = "REF-1", friendly_name_item = "Item", seuil_min_item = 5 };
            // Act
            var result = await service.CreateItem(dto);
            // Assert
            Assert.Equal("REF-1", result.reference_name_item);
            Assert.Equal(1, await context.Items.CountAsync());
            _fileService.Verify(f => f.CreateDirectory(It.IsAny<string>()), Times.Exactly(3));
            _itemHistoryService.Verify(h => h.LogHistory(result.id_item, null, ItemHistoryType.ItemCreated, null, null, null), Times.Once);
        }

        // --- UpdateItem ---

        [Fact]
        public async Task UpdateItem_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateItem(999, new UpdateItemDto());
            });
        }

        [Fact]
        public async Task UpdateItem_ShouldThrowInvalidOperationException_WhenReferenceNameAlreadyUsedByAnotherItem()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1, "REF-1"));
            context.Items.Add(BuildItem(2, "REF-2"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.UpdateItem(2, new UpdateItemDto { reference_name_item = "REF-1" });
            });
        }

        [Fact]
        public async Task UpdateItem_ShouldThrowKeyNotFoundException_WhenImgDoesNotBelongToItem()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1, "REF-1"));
            context.Items.Add(BuildItem(2, "REF-2"));
            context.Imgs.Add(BuildImg(1, 2));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateItem(1, new UpdateItemDto { id_img = 1 });
            });
        }

        [Fact]
        public async Task UpdateItem_ShouldUpdateFieldsAndLogHistory_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1, "REF-1", "Old name"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.UpdateItem(1, new UpdateItemDto { friendly_name_item = "New name" });
            // Assert
            Assert.Equal("New name", result.friendly_name_item);
            _itemHistoryService.Verify(h => h.LogHistory(1, null, ItemHistoryType.ItemUpdated, null, null, null), Times.Once);
        }

        // --- DeleteItem ---

        [Fact]
        public async Task DeleteItem_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeleteItem(999);
            });
        }

        [Fact]
        public async Task DeleteItem_ShouldDeleteItemDirectoriesAndLogHistory_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1, "REF-1"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            await service.DeleteItem(1);
            // Assert
            Assert.Equal(0, await context.Items.CountAsync());
            _fileService.Verify(f => f.DeleteDirectory(It.IsAny<string>()), Times.Exactly(3));
            _itemHistoryService.Verify(h => h.LogHistory(1, null, ItemHistoryType.ItemDeleted, null, null, null), Times.Once);
        }
    }
}
