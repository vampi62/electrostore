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
        private readonly Mock<IItemHistoryService> _itemHistoryService = new();

        private ItemBoxService CreateService(ApplicationDbContext context) =>
            new(_mapper, context, _itemHistoryService.Object);

        private static Stores BuildStore(string name = "store") => new()
        {
            name_store = name,
            mqtt_name_store = name + "-mqtt"
        };

        private static Boxs BuildBox(int storeId) => new()
        {
            id_store = storeId,
            xstart_box = 0,
            ystart_box = 0,
            xend_box = 5,
            yend_box = 5
        };

        private static Items BuildItem(string reference = "item") => new()
        {
            reference_name_item = reference,
            friendly_name_item = reference,
            threshold_min_item = 0
        };

        // --- GetItemsBoxsByBoxId ---

        [Fact]
        public async Task GetItemsBoxsByBoxId_ShouldReturnItemsLinkedToBox()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = BuildBox(store.id_store);
            var item = BuildItem();
            context.Boxs.Add(box);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            context.ItemsBoxs.Add(new ItemsBoxs { id_box = box.id_box, id_item = item.id_item, quantity_item_box = 5 });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetItemsBoxsByBoxId(box.id_box);

            var single = Assert.Single(result.data);
            Assert.Equal(item.id_item, single.id_item);
        }

        [Fact]
        public async Task GetItemsBoxsByBoxId_ShouldThrowKeyNotFoundException_WhenBoxDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetItemsBoxsByBoxId(999));
        }

        // --- GetItemsBoxsByItemId ---

        [Fact]
        public async Task GetItemsBoxsByItemId_ShouldReturnBoxesLinkedToItem()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = BuildBox(store.id_store);
            var item = BuildItem();
            context.Boxs.Add(box);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            context.ItemsBoxs.Add(new ItemsBoxs { id_box = box.id_box, id_item = item.id_item, quantity_item_box = 5 });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetItemsBoxsByItemId(item.id_item);

            var single = Assert.Single(result.data);
            Assert.Equal(box.id_box, single.id_box);
        }

        [Fact]
        public async Task GetItemsBoxsByItemId_ShouldThrowKeyNotFoundException_WhenItemDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetItemsBoxsByItemId(999));
        }

        // --- GetItemBoxById ---

        [Fact]
        public async Task GetItemBoxById_ShouldReturnItemBox_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = BuildBox(store.id_store);
            var item = BuildItem();
            context.Boxs.Add(box);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            context.ItemsBoxs.Add(new ItemsBoxs { id_box = box.id_box, id_item = item.id_item, quantity_item_box = 5 });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetItemBoxById(item.id_item, box.id_box);

            Assert.Equal(item.id_item, result.id_item);
            Assert.Equal(box.id_box, result.id_box);
        }

        [Fact]
        public async Task GetItemBoxById_ShouldThrowKeyNotFoundException_WhenLinkDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetItemBoxById(1, 1));
        }

        // --- CreateItemBox ---

        [Fact]
        public async Task CreateItemBox_ShouldPersistLink_AndLogStockAddedHistory()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = BuildBox(store.id_store);
            var item = BuildItem();
            context.Boxs.Add(box);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateItemBoxDto { id_box = box.id_box, id_item = item.id_item, quantity_item_box = 10, threshold_max_item_item_box = 20 };

            var result = await service.CreateItemBox(dto);

            Assert.Equal(10, result.quantity_item_box);
            Assert.Equal(1, await context.ItemsBoxs.CountAsync());
            _itemHistoryService.Verify(h => h.LogHistory(item.id_item, box.id_box, ItemHistoryType.StockAdded, null, 10, null), Times.Once);
        }

        [Fact]
        public async Task CreateItemBox_ShouldThrowKeyNotFoundException_WhenBoxDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem();
            context.Items.Add(item);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateItemBoxDto { id_box = 999, id_item = item.id_item, quantity_item_box = 1, threshold_max_item_item_box = 1 };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateItemBox(dto));
        }

        [Fact]
        public async Task CreateItemBox_ShouldThrowKeyNotFoundException_WhenItemDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = BuildBox(store.id_store);
            context.Boxs.Add(box);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateItemBoxDto { id_box = box.id_box, id_item = 999, quantity_item_box = 1, threshold_max_item_item_box = 1 };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateItemBox(dto));
        }

        [Fact]
        public async Task CreateItemBox_ShouldThrowInvalidOperationException_WhenItemAlreadyInBox()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = BuildBox(store.id_store);
            var item = BuildItem();
            context.Boxs.Add(box);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            context.ItemsBoxs.Add(new ItemsBoxs { id_box = box.id_box, id_item = item.id_item, quantity_item_box = 1 });
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateItemBoxDto { id_box = box.id_box, id_item = item.id_item, quantity_item_box = 1, threshold_max_item_item_box = 1 };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateItemBox(dto));
        }

        // --- UpdateItemBox ---

        [Fact]
        public async Task UpdateItemBox_ShouldLogStockAddedHistory_WhenQuantityIncreases()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = BuildBox(store.id_store);
            var item = BuildItem();
            context.Boxs.Add(box);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            context.ItemsBoxs.Add(new ItemsBoxs { id_box = box.id_box, id_item = item.id_item, quantity_item_box = 5 });
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new UpdateItemBoxDto { quantity_item_box = 10 };

            var result = await service.UpdateItemBox(item.id_item, box.id_box, dto);

            Assert.Equal(10, result.quantity_item_box);
            _itemHistoryService.Verify(h => h.LogHistory(item.id_item, box.id_box, ItemHistoryType.StockAdded, 5, 10, null), Times.Once);
        }

        [Fact]
        public async Task UpdateItemBox_ShouldLogStockRemovedHistory_WhenQuantityDecreases()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = BuildBox(store.id_store);
            var item = BuildItem();
            context.Boxs.Add(box);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            context.ItemsBoxs.Add(new ItemsBoxs { id_box = box.id_box, id_item = item.id_item, quantity_item_box = 5 });
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new UpdateItemBoxDto { quantity_item_box = 2 };

            var result = await service.UpdateItemBox(item.id_item, box.id_box, dto);

            Assert.Equal(2, result.quantity_item_box);
            _itemHistoryService.Verify(h => h.LogHistory(item.id_item, box.id_box, ItemHistoryType.StockRemoved, 5, 2, null), Times.Once);
        }

        [Fact]
        public async Task UpdateItemBox_ShouldNotLogHistory_WhenQuantityIsNotChanged()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = BuildBox(store.id_store);
            var item = BuildItem();
            context.Boxs.Add(box);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            context.ItemsBoxs.Add(new ItemsBoxs { id_box = box.id_box, id_item = item.id_item, quantity_item_box = 5, threshold_max_item_item_box = 1 });
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new UpdateItemBoxDto { threshold_max_item_item_box = 50 };

            var result = await service.UpdateItemBox(item.id_item, box.id_box, dto);

            Assert.Equal(50, result.threshold_max_item_item_box);
            _itemHistoryService.Verify(h => h.LogHistory(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<ItemHistoryType>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task UpdateItemBox_ShouldThrowKeyNotFoundException_WhenLinkDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateItemBox(1, 1, new UpdateItemBoxDto()));
        }

        // --- DeleteItemBox ---

        [Fact]
        public async Task DeleteItemBox_ShouldRemoveLink_AndLogStockRemovedHistory()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = BuildBox(store.id_store);
            var item = BuildItem();
            context.Boxs.Add(box);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            context.ItemsBoxs.Add(new ItemsBoxs { id_box = box.id_box, id_item = item.id_item, quantity_item_box = 5 });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.DeleteItemBox(item.id_item, box.id_box);

            Assert.Equal(0, await context.ItemsBoxs.CountAsync());
            _itemHistoryService.Verify(h => h.LogHistory(item.id_item, box.id_box, ItemHistoryType.StockRemoved, 5, null, null), Times.Once);
        }

        [Fact]
        public async Task DeleteItemBox_ShouldThrowKeyNotFoundException_WhenLinkDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteItemBox(1, 1));
        }
    }
}
