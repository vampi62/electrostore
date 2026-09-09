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
using ElectrostoreAPI.Services.BoxService;
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Services.ValidateStoreService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class BoxServiceTests : TestBase
    {
        private readonly Mock<ISessionService> _sessionService = new();

        public BoxServiceTests()
        {
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Admin);
        }

        private BoxService CreateService(ApplicationDbContext context) =>
            new(_mapper, context, _sessionService.Object, new ValidateStoreService(context));

        private static Stores BuildStore(string name = "store", int xlength = 100, int ylength = 100) => new()
        {
            name_store = name,
            mqtt_name_store = name + "-mqtt",
            xlength_store = xlength,
            ylength_store = ylength
        };

        // --- GetBoxsByStoreId ---

        [Fact]
        public async Task GetBoxsByStoreId_ShouldReturnBoxesForStore()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            context.Boxs.Add(new Boxs { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 5, yend_box = 5 });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetBoxsByStoreId(store.id_store);

            Assert.Single(result.data);
        }

        [Fact]
        public async Task GetBoxsByStoreId_ShouldThrowKeyNotFoundException_WhenStoreDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetBoxsByStoreId(999));
        }

        // --- GetBoxById ---

        [Fact]
        public async Task GetBoxById_ShouldReturnBox_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = new Boxs { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 5, yend_box = 5 };
            context.Boxs.Add(box);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetBoxById(box.id_box);

            Assert.Equal(box.id_box, result.id_box);
        }

        [Fact]
        public async Task GetBoxById_ShouldThrowKeyNotFoundException_WhenBoxDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetBoxById(999));
        }

        // --- CreateBox ---

        [Fact]
        public async Task CreateBox_ShouldPersistBox_WhenClientIsAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateBoxDto { xstart_box = 0, ystart_box = 0, xend_box = 5, yend_box = 5, id_store = store.id_store };

            var result = await service.CreateBox(dto);

            Assert.Equal(1, await context.Boxs.CountAsync());
            Assert.Equal(store.id_store, result.id_store);
        }

        [Fact]
        public async Task CreateBox_ShouldThrowArgumentException_WhenPositionOverlapsExistingBox()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            context.Boxs.Add(new Boxs { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 5, yend_box = 5 });
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateBoxDto { xstart_box = 2, ystart_box = 2, xend_box = 7, yend_box = 7, id_store = store.id_store };

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateBox(dto));
        }

        [Fact]
        public async Task CreateBox_ShouldThrowArgumentException_WhenPositionOutOfStoreBounds()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore(xlength: 10, ylength: 10);
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateBoxDto { xstart_box = 0, ystart_box = 0, xend_box = 50, yend_box = 5, id_store = store.id_store };

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateBox(dto));
        }

        [Fact]
        public async Task CreateBox_ShouldThrowKeyNotFoundException_WhenStoreDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateBoxDto { xstart_box = 0, ystart_box = 0, xend_box = 5, yend_box = 5, id_store = 999 };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateBox(dto));
        }

        [Fact]
        public async Task CreateBox_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);
            var dto = new CreateBoxDto { xstart_box = 0, ystart_box = 0, xend_box = 5, yend_box = 5, id_store = store.id_store };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateBox(dto));
        }

        // --- UpdateBox ---

        [Fact]
        public async Task UpdateBox_ShouldUpdateProvidedFields_WhenClientIsAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = new Boxs { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 5, yend_box = 5 };
            context.Boxs.Add(box);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new UpdateBoxDto { xend_box = 8 };

            var result = await service.UpdateBox(box.id_box, dto);

            Assert.Equal(8, result.xend_box);
        }

        [Fact]
        public async Task UpdateBox_ShouldThrowKeyNotFoundException_WhenBoxDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateBox(999, new UpdateBoxDto()));
        }

        [Fact]
        public async Task UpdateBox_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = new Boxs { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 5, yend_box = 5 };
            context.Boxs.Add(box);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateBox(box.id_box, new UpdateBoxDto { xend_box = 6 }));
        }

        // --- DeleteBox ---

        [Fact]
        public async Task DeleteBox_ShouldRemoveBox_WhenClientIsAdmin_AndBoxIsEmpty()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = new Boxs { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 5, yend_box = 5 };
            context.Boxs.Add(box);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.DeleteBox(box.id_box);

            Assert.Equal(0, await context.Boxs.CountAsync());
        }

        [Fact]
        public async Task DeleteBox_ShouldThrowInvalidOperationException_WhenBoxHasItems()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = new Boxs { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 5, yend_box = 5 };
            context.Boxs.Add(box);
            await context.SaveChangesAsync();
            var item = new Items { reference_name_item = "item", friendly_name_item = "item", threshold_min_item = 0 };
            context.Items.Add(item);
            await context.SaveChangesAsync();
            context.ItemsBoxs.Add(new ItemsBoxs { id_box = box.id_box, id_item = item.id_item, quantity_item_box = 3 });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteBox(box.id_box));
            Assert.Equal(1, await context.Boxs.CountAsync());
        }

        [Fact]
        public async Task DeleteBox_ShouldThrowKeyNotFoundException_WhenBoxDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteBox(999));
        }

        [Fact]
        public async Task DeleteBox_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = new Boxs { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 5, yend_box = 5 };
            context.Boxs.Add(box);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteBox(box.id_box));
            Assert.Equal(1, await context.Boxs.CountAsync());
        }
    }
}
