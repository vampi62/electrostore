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
using ElectrostoreAPI.Services.ItemBoxService;
using ElectrostoreAPI.Services.ItemHistoryService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class ItemBoxServiceTests : TestBase
    {
        private readonly Mock<IItemHistoryService> _itemHistoryService;

        public ItemBoxServiceTests()
        {
            _itemHistoryService = new Mock<IItemHistoryService>();
        }

        private ItemBoxService CreateService(ApplicationDbContext context)
        {
            return new ItemBoxService(_mapper, context, _itemHistoryService.Object);
        }

        private static Stores BuildStore(int id, string name = "Store")
        {
            return new Stores
            {
                id_store = id,
                nom_store = name,
                mqtt_name_store = "mqtt-" + id
            };
        }

        private static Boxs BuildBox(int id, int storeId)
        {
            return new Boxs
            {
                id_box = id,
                id_store = storeId,
                xend_box = 10,
                yend_box = 10
            };
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

        private static ItemsBoxs BuildItemBox(int itemId, int boxId, int qty = 5)
        {
            return new ItemsBoxs
            {
                id_item = itemId,
                id_box = boxId,
                qte_item_box = qty
            };
        }

        // --- GetItemsBoxsByBoxId ---

        [Fact]
        public async Task GetItemsBoxsByBoxId_ShouldThrowKeyNotFoundException_WhenBoxNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetItemsBoxsByBoxId(1);
            });
        }

        [Fact]
        public async Task GetItemsBoxsByBoxId_ShouldReturnOnlyItemsForGivenBox()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            context.Boxs.Add(BuildBox(2, 1));
            context.Items.Add(BuildItem(1));
            context.ItemsBoxs.Add(BuildItemBox(1, 1));
            context.ItemsBoxs.Add(BuildItemBox(1, 2));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetItemsBoxsByBoxId(1);
            // Assert
            var entry = Assert.Single(result.data);
            Assert.Equal(1, entry.id_box);
        }

        // --- GetItemsBoxsByItemId ---

        [Fact]
        public async Task GetItemsBoxsByItemId_ShouldThrowKeyNotFoundException_WhenItemNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetItemsBoxsByItemId(1);
            });
        }

        [Fact]
        public async Task GetItemsBoxsByItemId_ShouldReturnOnlyBoxesForGivenItem()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            context.Items.Add(BuildItem(1));
            context.Items.Add(BuildItem(2));
            context.ItemsBoxs.Add(BuildItemBox(1, 1));
            context.ItemsBoxs.Add(BuildItemBox(2, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetItemsBoxsByItemId(1);
            // Assert
            var entry = Assert.Single(result.data);
            Assert.Equal(1, entry.id_item);
        }

        // --- GetItemBoxById ---

        [Fact]
        public async Task GetItemBoxById_ShouldReturnEntry_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            context.Items.Add(BuildItem(1));
            context.ItemsBoxs.Add(BuildItemBox(1, 1, 7));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetItemBoxById(1, 1);
            // Assert
            Assert.Equal(7, result.qte_item_box);
        }

        [Fact]
        public async Task GetItemBoxById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetItemBoxById(999, 999);
            });
        }

        // --- CreateItemBox ---

        [Fact]
        public async Task CreateItemBox_ShouldThrowKeyNotFoundException_WhenBoxNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateItemBoxDto { id_box = 999, id_item = 1, qte_item_box = 5, seuil_max_item_item_box = 10 };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreateItemBox(dto);
            });
        }

        [Fact]
        public async Task CreateItemBox_ShouldThrowKeyNotFoundException_WhenItemNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateItemBoxDto { id_box = 1, id_item = 999, qte_item_box = 5, seuil_max_item_item_box = 10 };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreateItemBox(dto);
            });
        }

        [Fact]
        public async Task CreateItemBox_ShouldThrowInvalidOperationException_WhenAlreadyExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            context.Items.Add(BuildItem(1));
            context.ItemsBoxs.Add(BuildItemBox(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateItemBoxDto { id_box = 1, id_item = 1, qte_item_box = 5, seuil_max_item_item_box = 10 };
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.CreateItemBox(dto);
            });
        }

        [Fact]
        public async Task CreateItemBox_ShouldCreateEntryAndLogStockAdded_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            context.Items.Add(BuildItem(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateItemBoxDto { id_box = 1, id_item = 1, qte_item_box = 10, seuil_max_item_item_box = 20 };
            // Act
            var result = await service.CreateItemBox(dto);
            // Assert
            Assert.Equal(10, result.qte_item_box);
            _itemHistoryService.Verify(h => h.LogHistory(1, 1, ItemHistoryType.StockAdded, null, 10, null), Times.Once);
        }

        // --- UpdateItemBox ---

        [Fact]
        public async Task UpdateItemBox_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateItemBox(999, 999, new UpdateItemBoxDto());
            });
        }

        [Fact]
        public async Task UpdateItemBox_ShouldLogStockAdded_WhenQuantityIncreases()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            context.Items.Add(BuildItem(1));
            context.ItemsBoxs.Add(BuildItemBox(1, 1, 5));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.UpdateItemBox(1, 1, new UpdateItemBoxDto { qte_item_box = 8 });
            // Assert
            Assert.Equal(8, result.qte_item_box);
            _itemHistoryService.Verify(h => h.LogHistory(1, 1, ItemHistoryType.StockAdded, 5, 8, null), Times.Once);
        }

        [Fact]
        public async Task UpdateItemBox_ShouldLogStockRemoved_WhenQuantityDecreases()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            context.Items.Add(BuildItem(1));
            context.ItemsBoxs.Add(BuildItemBox(1, 1, 5));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.UpdateItemBox(1, 1, new UpdateItemBoxDto { qte_item_box = 2 });
            // Assert
            Assert.Equal(2, result.qte_item_box);
            _itemHistoryService.Verify(h => h.LogHistory(1, 1, ItemHistoryType.StockRemoved, 5, 2, null), Times.Once);
        }

        [Fact]
        public async Task UpdateItemBox_ShouldNotLogHistory_WhenQuantityNotProvided()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            context.Items.Add(BuildItem(1));
            context.ItemsBoxs.Add(BuildItemBox(1, 1, 5));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            await service.UpdateItemBox(1, 1, new UpdateItemBoxDto { seuil_max_item_item_box = 20 });
            // Assert
            _itemHistoryService.Verify(h => h.LogHistory(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<ItemHistoryType>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>()), Times.Never);
        }

        // --- DeleteItemBox ---

        [Fact]
        public async Task DeleteItemBox_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeleteItemBox(999, 999);
            });
        }

        [Fact]
        public async Task DeleteItemBox_ShouldDeleteEntryAndLogStockRemoved_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            context.Items.Add(BuildItem(1));
            context.ItemsBoxs.Add(BuildItemBox(1, 1, 5));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            await service.DeleteItemBox(1, 1);
            // Assert
            Assert.Equal(0, await context.ItemsBoxs.CountAsync());
            _itemHistoryService.Verify(h => h.LogHistory(1, 1, ItemHistoryType.StockRemoved, 5, null, null), Times.Once);
        }

        // --- CheckIfStoreExists ---

        [Fact]
        public async Task CheckIfStoreExists_ShouldThrowKeyNotFoundException_WhenBoxNotInStore()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Stores.Add(BuildStore(2));
            context.Boxs.Add(BuildBox(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CheckIfStoreExists(2, 1);
            });
        }

        [Fact]
        public async Task CheckIfStoreExists_ShouldNotThrow_WhenBoxBelongsToStore()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var exception = await Record.ExceptionAsync(async () =>
            {
                await service.CheckIfStoreExists(1, 1);
            });
            // Assert
            Assert.Null(exception);
        }
    }
}
