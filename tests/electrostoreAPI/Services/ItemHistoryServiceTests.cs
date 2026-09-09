using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElectrostoreAPI;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.ItemHistoryService;
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class ItemHistoryServiceTests : TestBase
    {
        private readonly Mock<ISessionService> _sessionService = new();

        private ItemHistoryService CreateService(ApplicationDbContext context) =>
            new(_mapper, context, _sessionService.Object);

        private static Items BuildItem(string reference = "item") => new()
        {
            reference_name_item = reference,
            friendly_name_item = reference,
            threshold_min_item = 0
        };

        // --- GetItemHistoryByItemId ---

        [Fact]
        public async Task GetItemHistoryByItemId_ShouldReturnHistoryForItem()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem();
            context.Items.Add(item);
            await context.SaveChangesAsync();
            context.ItemsHistory.Add(new ItemsHistory { id_item = item.id_item, type_item_history = ItemHistoryType.ItemCreated });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetItemHistoryByItemId(item.id_item);

            Assert.Single(result.data);
        }

        [Fact]
        public async Task GetItemHistoryByItemId_ShouldThrowKeyNotFoundException_WhenItemDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetItemHistoryByItemId(999));
        }

        // --- GetItemHistoryById ---

        [Fact]
        public async Task GetItemHistoryById_ShouldReturnEntry_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem();
            context.Items.Add(item);
            await context.SaveChangesAsync();
            var history = new ItemsHistory { id_item = item.id_item, type_item_history = ItemHistoryType.ItemUpdated };
            context.ItemsHistory.Add(history);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetItemHistoryById(history.id_item_history, item.id_item);

            Assert.Equal(history.id_item_history, result.id_item_history);
        }

        [Fact]
        public async Task GetItemHistoryById_ShouldThrowKeyNotFoundException_WhenItemIdDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem();
            context.Items.Add(item);
            await context.SaveChangesAsync();
            var history = new ItemsHistory { id_item = item.id_item, type_item_history = ItemHistoryType.ItemUpdated };
            context.ItemsHistory.Add(history);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetItemHistoryById(history.id_item_history, item.id_item + 1));
        }

        [Fact]
        public async Task GetItemHistoryById_ShouldThrowKeyNotFoundException_WhenEntryDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetItemHistoryById(999, 1));
        }

        // --- GetItemsHistory ---

        [Fact]
        public async Task GetItemsHistory_ShouldReturnAllHistoryEntries_WhenNoFilterApplied()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem();
            context.Items.Add(item);
            await context.SaveChangesAsync();
            context.ItemsHistory.AddRange(
                new ItemsHistory { id_item = item.id_item, type_item_history = ItemHistoryType.ItemCreated },
                new ItemsHistory { id_item = item.id_item, type_item_history = ItemHistoryType.ItemUpdated });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetItemsHistory();

            Assert.Equal(2, result.pagination.total);
        }

        // --- GetItemsHistoryByPeriodAsync ---

        [Fact]
        public async Task GetItemsHistoryByPeriodAsync_ShouldReturnOnlyEntriesWithinPeriod()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem();
            context.Items.Add(item);
            await context.SaveChangesAsync();
            var insidePeriod = new ItemsHistory { id_item = item.id_item, type_item_history = ItemHistoryType.ItemCreated };
            var outsidePeriod = new ItemsHistory { id_item = item.id_item, type_item_history = ItemHistoryType.ItemUpdated };
            context.ItemsHistory.AddRange(insidePeriod, outsidePeriod);
            await context.SaveChangesAsync();
            insidePeriod.created_at = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc);
            outsidePeriod.created_at = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = (await service.GetItemsHistoryByPeriodAsync(
                new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc))).ToList();

            var single = Assert.Single(result);
            Assert.Equal(insidePeriod.id_item_history, single.id_item_history);
        }

        // --- LogHistory ---

        [Fact]
        public async Task LogHistory_ShouldPersistEntry_WithClientIdAndComputedQuantityChange()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientId()).Returns(42);
            var service = CreateService(context);

            await service.LogHistory(1, 2, ItemHistoryType.StockUpdated, oldQuantity: 5, newQuantity: 8);

            var entry = Assert.Single(context.ItemsHistory);
            Assert.Equal(1, entry.id_item);
            Assert.Equal(2, entry.id_box);
            Assert.Equal(42, entry.id_user);
            Assert.Equal(ItemHistoryType.StockUpdated, entry.type_item_history);
            Assert.Equal(3, entry.quantity_change_item_history);
        }

        [Fact]
        public async Task LogHistory_ShouldComputeNegativeQuantityChange_WhenOnlyOldQuantityProvided()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientId()).Returns(1);
            var service = CreateService(context);

            await service.LogHistory(1, 2, ItemHistoryType.StockRemoved, oldQuantity: 5, newQuantity: null);

            var entry = Assert.Single(context.ItemsHistory);
            Assert.Equal(-5, entry.quantity_change_item_history);
        }

        [Fact]
        public async Task LogHistory_ShouldComputePositiveQuantityChange_WhenOnlyNewQuantityProvided()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientId()).Returns(1);
            var service = CreateService(context);

            await service.LogHistory(1, 2, ItemHistoryType.StockAdded, oldQuantity: null, newQuantity: 5);

            var entry = Assert.Single(context.ItemsHistory);
            Assert.Equal(5, entry.quantity_change_item_history);
        }

        [Fact]
        public async Task LogHistory_ShouldLeaveQuantityChangeNull_WhenNoQuantitiesProvided()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientId()).Returns(1);
            var service = CreateService(context);

            await service.LogHistory(1, null, ItemHistoryType.ItemUpdated);

            var entry = Assert.Single(context.ItemsHistory);
            Assert.Null(entry.quantity_change_item_history);
        }
    }
}
