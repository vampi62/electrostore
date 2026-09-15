using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Services.ItemHistoryService;
using ElectrostoreAPI.Services.ItemService;
using ElectrostoreAPI.Tests.Utils;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ElectrostoreAPI.Tests.Services
{
    public class ItemServiceTests : TestBase
    {
        private readonly Mock<IFileService> _fileService = new();
        private readonly Mock<IItemHistoryService> _itemHistoryService = new();

        private ItemService CreateService(ApplicationDbContext context) =>
            new(_mapper, context, _fileService.Object, _itemHistoryService.Object);

        private static Items BuildItem(string reference, int threshold) => new()
        {
            reference_name_item = reference,
            friendly_name_item = reference,
            threshold_min_item = threshold
        };

        [Fact]
        public async Task GetLowStockItemsAsync_ShouldReturnOnlyItems_WithQuantityBelowThreshold()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var lowItem = BuildItem("LOW", 10);
            var okItem = BuildItem("OK", 10);
            context.Items.AddRange(lowItem, okItem);
            await context.SaveChangesAsync();
            context.ItemsBoxs.AddRange(
                new ItemsBoxs { id_item = lowItem.id_item, id_box = 1, quantity_item_box = 2 },
                new ItemsBoxs { id_item = okItem.id_item, id_box = 1, quantity_item_box = 20 });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            // Act
            var result = (await service.GetLowStockItemsAsync()).ToList();

            // Assert
            var single = Assert.Single(result);
            Assert.Equal("LOW", single.reference_name_item);
            Assert.Equal(2, single.quantity_item);
        }

        [Fact]
        public async Task GetLowStockItemsAsync_ShouldIgnoreItems_WithNoThresholdConfigured()
        {
            // Arrange - threshold_min_item == 0 means "no alert configured" for this item.
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem("NOALERT", 0);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            context.ItemsBoxs.Add(new ItemsBoxs { id_item = item.id_item, id_box = 1, quantity_item_box = 0 });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            // Act
            var result = await service.GetLowStockItemsAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetLowStockItemsAsync_ShouldReturnAllLowItems_WhenSinceDateIsNotProvided()
        {
            // Arrange - default "full summary" behaviour, regardless of history.
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem("LOW", 10);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            context.ItemsBoxs.Add(new ItemsBoxs { id_item = item.id_item, id_box = 1, quantity_item_box = 1 });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            // Act
            var result = await service.GetLowStockItemsAsync();

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public async Task GetLowStockItemsAsync_ShouldOnlyReturnItems_WithRecentQuantityChange_WhenSinceDateIsProvided()
        {
            // Arrange - two items below threshold, only one has a recent quantity-changing history entry.
            using var context = new ApplicationDbContext(_dbContextOptions);
            var recentlyChanged = BuildItem("RECENT", 10);
            var staleItem = BuildItem("STALE", 10);
            context.Items.AddRange(recentlyChanged, staleItem);
            await context.SaveChangesAsync();
            context.ItemsBoxs.AddRange(
                new ItemsBoxs { id_item = recentlyChanged.id_item, id_box = 1, quantity_item_box = 1 },
                new ItemsBoxs { id_item = staleItem.id_item, id_box = 1, quantity_item_box = 1 });
            var recentHistory = new ItemsHistory { id_item = recentlyChanged.id_item, type_item_history = ItemHistoryType.StockRemoved };
            var staleHistory = new ItemsHistory { id_item = staleItem.id_item, type_item_history = ItemHistoryType.StockRemoved };
            context.ItemsHistory.AddRange(recentHistory, staleHistory);
            await context.SaveChangesAsync();
            // ApplicationDbContext.AddTimestamps stamps created_at to "now" on insert, overriding
            // any value set beforehand - update it in a second save (State becomes Modified, which
            // only re-stamps updated_at) to simulate history entries from different dates.
            recentHistory.created_at = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
            staleHistory.created_at = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            // Act
            var result = (await service.GetLowStockItemsAsync(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc))).ToList();

            // Assert
            var single = Assert.Single(result);
            Assert.Equal("RECENT", single.reference_name_item);
        }

        [Fact]
        public async Task GetLowStockItemsAsync_ShouldIgnoreHistoryEntries_NotRelatedToQuantityChanges()
        {
            // Arrange - an item below threshold whose only recent history entry is a metadata
            // update (not a quantity change) must be excluded when sinceDate is provided.
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem("METADATA-ONLY", 10);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            context.ItemsBoxs.Add(new ItemsBoxs { id_item = item.id_item, id_box = 1, quantity_item_box = 1 });
            var history = new ItemsHistory { id_item = item.id_item, type_item_history = ItemHistoryType.ItemUpdated };
            context.ItemsHistory.Add(history);
            await context.SaveChangesAsync();
            history.created_at = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            // Act
            var result = await service.GetLowStockItemsAsync(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));

            // Assert
            Assert.Empty(result);
        }

        // --- GetItems ---

        [Fact]
        public async Task GetItems_ShouldReturnAllItems_WhenNoFilterApplied()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.AddRange(BuildItem("a", 10), BuildItem("b", 10));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            // Act
            var result = await service.GetItems();

            // Assert
            Assert.Equal(2, result.pagination.total);
            Assert.Equal(2, result.data.Count());
        }

        // --- GetItemById ---

        [Fact]
        public async Task GetItemById_ShouldReturnItem_WhenItExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem("a", 10);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            // Act
            var result = await service.GetItemById(item.id_item);

            // Assert
            Assert.Equal(item.id_item, result.id_item);
            Assert.Equal("a", result.reference_name_item);
        }

        [Fact]
        public async Task GetItemById_ShouldThrowKeyNotFoundException_WhenItemDoesNotExist()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetItemById(999));
        }

        // --- CreateItem ---

        [Fact]
        public async Task CreateItem_ShouldPersistItem_AndCreateDirectories()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateItemDto { reference_name_item = "new-item", friendly_name_item = "New item", threshold_min_item = 5 };

            // Act
            var result = await service.CreateItem(dto);

            // Assert
            Assert.Equal("new-item", result.reference_name_item);
            Assert.Equal(1, await context.Items.CountAsync());
            _fileService.Verify(f => f.CreateDirectory(It.IsAny<string>()), Times.Exactly(3));
            _itemHistoryService.Verify(h => h.LogHistory(result.id_item, null, ItemHistoryType.ItemCreated, null, null, null), Times.Once);
        }

        [Fact]
        public async Task CreateItem_ShouldThrowInvalidOperationException_WhenReferenceNameAlreadyExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem("dup", 10));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateItemDto { reference_name_item = "dup", friendly_name_item = "Dup item", threshold_min_item = 5 };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateItem(dto));
        }

        // --- UpdateItem ---

        [Fact]
        public async Task UpdateItem_ShouldUpdateProvidedFields()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem("a", 10);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new UpdateItemDto { friendly_name_item = "renamed", threshold_min_item = 20 };

            // Act
            var result = await service.UpdateItem(item.id_item, dto);

            // Assert
            Assert.Equal("renamed", result.friendly_name_item);
            Assert.Equal(20, result.threshold_min_item);
            Assert.Equal("a", result.reference_name_item);
            _itemHistoryService.Verify(h => h.LogHistory(item.id_item, null, ItemHistoryType.ItemUpdated, null, null, null), Times.Once);
        }

        [Fact]
        public async Task UpdateItem_ShouldThrowInvalidOperationException_WhenNewReferenceNameAlreadyUsedByAnotherItem()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item1 = BuildItem("a", 10);
            var item2 = BuildItem("b", 10);
            context.Items.AddRange(item1, item2);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new UpdateItemDto { reference_name_item = "b" };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateItem(item1.id_item, dto));
        }

        [Fact]
        public async Task UpdateItem_ShouldThrowKeyNotFoundException_WhenItemDoesNotExist()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateItem(999, new UpdateItemDto()));
        }

        // --- DeleteItem ---

        [Fact]
        public async Task DeleteItem_ShouldRemoveItem_AndDeleteDirectories()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem("a", 10);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            // Act
            await service.DeleteItem(item.id_item);

            // Assert
            Assert.Equal(0, await context.Items.CountAsync());
            _fileService.Verify(f => f.DeleteDirectory(It.IsAny<string>()), Times.Exactly(3));
            _itemHistoryService.Verify(h => h.LogHistory(item.id_item, null, ItemHistoryType.ItemDeleted, null, null, null), Times.Once);
        }

        [Fact]
        public async Task DeleteItem_ShouldThrowKeyNotFoundException_WhenItemDoesNotExist()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteItem(999));
        }
    }
}
