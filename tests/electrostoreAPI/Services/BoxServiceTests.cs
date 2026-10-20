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

        // --- GetBoxsByStoreId / GetBoxById: counts and expansions ---

        [Fact]
        public async Task GetBoxsByStoreId_ShouldReturnCountsAndExpandedRelations()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            var tag = new Tags { name_tag = "tag" };
            var item = new Items { reference_name_item = "r", friendly_name_item = "r", threshold_min_item = 0 };
            context.Stores.Add(store);
            context.Tags.Add(tag);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            var box = new Boxs { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 5, yend_box = 5 };
            context.Boxs.Add(box);
            await context.SaveChangesAsync();
            context.BoxsTags.Add(new BoxsTags { id_box = box.id_box, id_tag = tag.id_tag });
            context.ItemsBoxs.Add(new ItemsBoxs { id_box = box.id_box, id_item = item.id_item, quantity_item_box = 1, threshold_max_item_item_box = 5 });
            await context.SaveChangesAsync();
            using var queryContext = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(queryContext);

            var result = await service.GetBoxsByStoreId(store.id_store, expand: ["store", "box_tags", "item_boxs"]);

            var single = Assert.Single(result.data);
            Assert.Equal(1, single.box_tags_count);
            Assert.Equal(1, single.item_boxs_count);
            Assert.NotNull(single.store);
            Assert.Single(single.box_tags!);
            Assert.Single(single.item_boxs!);
        }

        [Fact]
        public async Task GetBoxById_ShouldReturnCountsAndExpandedRelations()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            var tag = new Tags { name_tag = "tag" };
            context.Stores.Add(store);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            var box = new Boxs { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 5, yend_box = 5 };
            context.Boxs.Add(box);
            await context.SaveChangesAsync();
            context.BoxsTags.Add(new BoxsTags { id_box = box.id_box, id_tag = tag.id_tag });
            await context.SaveChangesAsync();
            using var queryContext = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(queryContext);

            var result = await service.GetBoxById(box.id_box, store.id_store, ["store", "box_tags", "item_boxs"]);

            Assert.Equal(1, result.box_tags_count);
            Assert.NotNull(result.store);
            Assert.Single(result.box_tags!);
            Assert.Empty(result.item_boxs!);
        }

        [Fact]
        public async Task GetBoxById_ShouldThrowKeyNotFoundException_WhenStoreIdDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = new Boxs { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 5, yend_box = 5 };
            context.Boxs.Add(box);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetBoxById(box.id_box, store.id_store + 1));
        }

        // --- CreateBulkBox ---

        [Fact]
        public async Task CreateBulkBox_ShouldPersistAllBoxes_WhenAllAreValid()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.CreateBulkBox(
            [
                new CreateBoxDto { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 2, yend_box = 2 },
                new CreateBoxDto { id_store = store.id_store, xstart_box = 5, ystart_box = 5, xend_box = 7, yend_box = 7 }
            ]);

            Assert.Equal(2, result.valide.Count);
            Assert.Empty(result.error);
            Assert.Equal(2, await context.Boxs.CountAsync());
        }

        [Fact]
        public async Task CreateBulkBox_ShouldPersistNothing_WhenAnyBoxIsInvalid()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.CreateBulkBox(
            [
                new CreateBoxDto { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 2, yend_box = 2 },
                new CreateBoxDto { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 999, yend_box = 2 },
                new CreateBoxDto { id_store = 999, xstart_box = 0, ystart_box = 0, xend_box = 2, yend_box = 2 }
            ]);

            Assert.Single(result.valide);
            Assert.Equal(2, result.error.Count);
            Assert.Equal(0, await context.Boxs.CountAsync());
        }

        [Fact]
        public async Task CreateBulkBox_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Moderator);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateBulkBox([]));
        }

        // --- UpdateBox: store mismatch ---

        [Fact]
        public async Task UpdateBox_ShouldThrowKeyNotFoundException_WhenStoreIdDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = new Boxs { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 5, yend_box = 5 };
            context.Boxs.Add(box);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateBox(box.id_box, new UpdateBoxDto { xend_box = 6 }, store.id_store + 1));
        }

        [Fact]
        public async Task UpdateBox_ShouldThrowArgumentException_WhenNewPositionOverlapsAnotherBox()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = new Boxs { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 2, yend_box = 2 };
            var neighbour = new Boxs { id_store = store.id_store, xstart_box = 3, ystart_box = 0, xend_box = 5, yend_box = 2 };
            context.Boxs.AddRange(box, neighbour);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateBox(box.id_box, new UpdateBoxDto { xend_box = 4 }));
        }

        // --- UpdateBulkBox ---

        [Fact]
        public async Task UpdateBulkBox_ShouldPersistAllChanges_WhenAllAreValid()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var first = new Boxs { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 2, yend_box = 2 };
            var second = new Boxs { id_store = store.id_store, xstart_box = 5, ystart_box = 5, xend_box = 7, yend_box = 7 };
            context.Boxs.AddRange(first, second);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateBulkBox(
            [
                new UpdateBulkBoxByStoreDto { id_box = first.id_box, xend_box = 3 },
                new UpdateBulkBoxByStoreDto { id_box = second.id_box, yend_box = 8 }
            ], store.id_store);

            Assert.Equal(2, result.valide.Count);
            Assert.Empty(result.error);
            Assert.Equal(3, (await context.Boxs.FindAsync(first.id_box))!.xend_box);
            Assert.Equal(8, (await context.Boxs.FindAsync(second.id_box))!.yend_box);
        }

        [Fact]
        public async Task UpdateBulkBox_ShouldReportErrors_WhenBoxIsMissingOutsideStoreOrOutOfBounds()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            var other = BuildStore("other");
            context.Stores.AddRange(store, other);
            await context.SaveChangesAsync();
            var box = new Boxs { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 2, yend_box = 2 };
            var foreign = new Boxs { id_store = other.id_store, xstart_box = 0, ystart_box = 0, xend_box = 2, yend_box = 2 };
            context.Boxs.AddRange(box, foreign);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateBulkBox(
            [
                new UpdateBulkBoxByStoreDto { id_box = box.id_box, xend_box = 3 },
                new UpdateBulkBoxByStoreDto { id_box = foreign.id_box, xend_box = 3 },
                new UpdateBulkBoxByStoreDto { id_box = 999, xend_box = 3 },
                new UpdateBulkBoxByStoreDto { id_box = box.id_box, xend_box = 999 }
            ], store.id_store);

            Assert.Equal(3, result.error.Count);
            Assert.Single(result.valide);
        }

        [Fact]
        public async Task UpdateBulkBox_ShouldReportOverlapErrors_WhenAfterChangesBoxesOverlap()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var first = new Boxs { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 2, yend_box = 2 };
            var second = new Boxs { id_store = store.id_store, xstart_box = 3, ystart_box = 0, xend_box = 5, yend_box = 2 };
            context.Boxs.AddRange(first, second);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateBulkBox([new UpdateBulkBoxByStoreDto { id_box = first.id_box, xend_box = 4 }]);

            Assert.NotEmpty(result.error);
        }

        [Fact]
        public async Task UpdateBulkBox_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateBulkBox([]));
        }

        // --- DeleteBox: store mismatch / DeleteBulkBox ---

        [Fact]
        public async Task DeleteBox_ShouldThrowKeyNotFoundException_WhenStoreIdDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = new Boxs { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 5, yend_box = 5 };
            context.Boxs.Add(box);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteBox(box.id_box, store.id_store + 1));
        }

        [Fact]
        public async Task DeleteBulkBox_ShouldReturnValidAndErrorEntries()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            var item = new Items { reference_name_item = "r", friendly_name_item = "r", threshold_min_item = 0 };
            context.Stores.Add(store);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            var empty = new Boxs { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 2, yend_box = 2 };
            var filled = new Boxs { id_store = store.id_store, xstart_box = 3, ystart_box = 0, xend_box = 5, yend_box = 2 };
            context.Boxs.AddRange(empty, filled);
            await context.SaveChangesAsync();
            context.ItemsBoxs.Add(new ItemsBoxs { id_box = filled.id_box, id_item = item.id_item, quantity_item_box = 3, threshold_max_item_item_box = 5 });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.DeleteBulkBox([empty.id_box, filled.id_box, 999], store.id_store);

            var deleted = Assert.Single(result.valide);
            Assert.Equal(empty.id_box, deleted.id_box);
            Assert.Equal(2, result.error.Count);
            Assert.Equal(1, await context.Boxs.CountAsync());
        }

        [Fact]
        public async Task DeleteBulkBox_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteBulkBox([1], 1));
        }
    }
}
