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
using ElectrostoreAPI.Services.ItemHistoryService;
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class ItemHistoryServiceTests : TestBase
    {
        private readonly Mock<ISessionService> _sessionService;

        public ItemHistoryServiceTests()
        {
            _sessionService = new Mock<ISessionService>();
        }

        private ItemHistoryService CreateService(ApplicationDbContext context)
        {
            return new ItemHistoryService(_mapper, context, _sessionService.Object);
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

        private static ItemsHistory BuildItemHistory(int id, int itemId, ItemHistoryType type = ItemHistoryType.StockAdded)
        {
            return new ItemsHistory
            {
                id_item_history = id,
                id_item = itemId,
                type = type
            };
        }

        // --- GetItemHistoryByItemId ---

        [Fact]
        public async Task GetItemHistoryByItemId_ShouldThrowKeyNotFoundException_WhenItemNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetItemHistoryByItemId(1);
            });
        }

        [Fact]
        public async Task GetItemHistoryByItemId_ShouldReturnOnlyHistoryForGivenItem()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.Items.Add(BuildItem(2));
            context.ItemsHistory.Add(BuildItemHistory(1, 1));
            context.ItemsHistory.Add(BuildItemHistory(2, 1));
            context.ItemsHistory.Add(BuildItemHistory(3, 2));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetItemHistoryByItemId(1);
            // Assert
            Assert.Equal(2, result.data.Count());
            Assert.Equal(2, result.pagination.total);
        }

        // --- GetItemHistoryById ---

        [Fact]
        public async Task GetItemHistoryById_ShouldReturnEntry_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.ItemsHistory.Add(BuildItemHistory(1, 1, ItemHistoryType.StockRemoved));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetItemHistoryById(1, 1);
            // Assert
            Assert.Equal(ItemHistoryType.StockRemoved, result.type);
        }

        [Fact]
        public async Task GetItemHistoryById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetItemHistoryById(999, 1);
            });
        }

        // --- GetItemsHistory ---

        [Fact]
        public async Task GetItemsHistory_ShouldReturnAll_Paginated()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.ItemsHistory.Add(BuildItemHistory(1, 1));
            context.ItemsHistory.Add(BuildItemHistory(2, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetItemsHistory();
            // Assert
            Assert.Equal(2, result.data.Count());
            Assert.Equal(2, result.pagination.total);
        }

        // --- LogHistory ---

        [Fact]
        public async Task LogHistory_ShouldComputeQuantityChange_WhenBothOldAndNewProvided()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientId()).Returns(5);
            var service = CreateService(context);
            // Act
            await service.LogHistory(1, null, ItemHistoryType.StockUpdated, oldQuantity: 10, newQuantity: 15);
            // Assert
            var entry = Assert.Single(context.ItemsHistory);
            Assert.Equal(5, entry.quantity_change);
            Assert.Equal(5, entry.id_user);
        }

        [Fact]
        public async Task LogHistory_ShouldUseNewQuantityAsChange_WhenOnlyNewProvided()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientId()).Returns(1);
            var service = CreateService(context);
            // Act
            await service.LogHistory(1, null, ItemHistoryType.StockAdded, newQuantity: 20);
            // Assert
            var entry = Assert.Single(context.ItemsHistory);
            Assert.Equal(20, entry.quantity_change);
        }

        [Fact]
        public async Task LogHistory_ShouldUseNegativeOldQuantityAsChange_WhenOnlyOldProvided()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientId()).Returns(1);
            var service = CreateService(context);
            // Act
            await service.LogHistory(1, null, ItemHistoryType.StockRemoved, oldQuantity: 8);
            // Assert
            var entry = Assert.Single(context.ItemsHistory);
            Assert.Equal(-8, entry.quantity_change);
        }

        [Fact]
        public async Task LogHistory_ShouldPersistBoxAndNotes()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientId()).Returns(1);
            var service = CreateService(context);
            // Act
            await service.LogHistory(1, 2, ItemHistoryType.ItemCreated, notes: "Initial stock");
            // Assert
            var entry = Assert.Single(context.ItemsHistory);
            Assert.Equal(2, entry.id_box);
            Assert.Equal("Initial stock", entry.notes);
            Assert.Null(entry.quantity_change);
        }
    }
}
