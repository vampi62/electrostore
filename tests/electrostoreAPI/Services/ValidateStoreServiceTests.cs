using Microsoft.EntityFrameworkCore;
using Xunit;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.ValidateStoreService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class ValidateStoreServiceTests : TestBase
    {
        private ValidateStoreService CreateService(ApplicationDbContext context) => new(context);

        private static Stores BuildStore(int xlength = 10, int ylength = 8, StorePositionMode mode = StorePositionMode.Grid) => new()
        {
            name_store = "store",
            mqtt_name_store = "store-mqtt",
            xlength_store = xlength,
            ylength_store = ylength,
            position_mode_store = mode
        };

        private static Boxs BuildBox(int storeId, int xs, int ys, int xe, int ye) => new()
        {
            id_store = storeId,
            xstart_box = xs,
            ystart_box = ys,
            xend_box = xe,
            yend_box = ye
        };

        // --- ValidateLedPosition ---

        [Theory]
        [InlineData(0, 0)]
        [InlineData(9, 7)]
        public void ValidateLedPosition_ShouldAccept_WhenLedIsInsideGridStore(int x, int y)
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            service.ValidateLedPosition(new Leds { x_led = x, y_led = y }, BuildStore());
        }

        [Theory]
        [InlineData(10, 0)]
        [InlineData(0, 8)]
        public void ValidateLedPosition_ShouldThrowArgumentException_WhenLedIsOutsideGridStore(int x, int y)
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            Assert.Throws<ArgumentException>(() => service.ValidateLedPosition(new Leds { x_led = x, y_led = y }, BuildStore()));
        }

        [Theory]
        [InlineData(LedBorderSide.Left, 7)]
        [InlineData(LedBorderSide.Right, 7)]
        [InlineData(LedBorderSide.Top, 9)]
        [InlineData(LedBorderSide.Bottom, 9)]
        public void ValidateLedPosition_ShouldAccept_WhenLedIsOnBorderWithinSideLength(LedBorderSide side, int position)
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            service.ValidateLedPosition(new Leds { x_led = position, y_led = (int)side }, BuildStore(mode: StorePositionMode.Border));
        }

        [Theory]
        [InlineData(LedBorderSide.Left, 8)]
        [InlineData(LedBorderSide.Right, 8)]
        [InlineData(LedBorderSide.Top, 10)]
        [InlineData(LedBorderSide.Bottom, 10)]
        public void ValidateLedPosition_ShouldThrowArgumentException_WhenBorderLedExceedsSideLength(LedBorderSide side, int position)
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            Assert.Throws<ArgumentException>(() =>
                service.ValidateLedPosition(new Leds { x_led = position, y_led = (int)side }, BuildStore(mode: StorePositionMode.Border)));
        }

        [Fact]
        public void ValidateLedPosition_ShouldThrowArgumentException_WhenBorderSideIsUnknown()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            Assert.Throws<ArgumentException>(() =>
                service.ValidateLedPosition(new Leds { x_led = 0, y_led = 42 }, BuildStore(mode: StorePositionMode.Border)));
        }

        // --- ValidateBoxPosition ---

        [Fact]
        public void ValidateBoxPosition_ShouldAccept_WhenBoxIsInsideStore()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            service.ValidateBoxPosition(BuildBox(1, 0, 0, 10, 8), BuildStore());
        }

        [Theory]
        [InlineData(5, 0, 5, 3)]
        [InlineData(5, 0, 4, 3)]
        [InlineData(0, 5, 3, 5)]
        [InlineData(0, 5, 3, 4)]
        public void ValidateBoxPosition_ShouldThrowArgumentException_WhenEndIsNotAfterStart(int xs, int ys, int xe, int ye)
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            Assert.Throws<ArgumentException>(() => service.ValidateBoxPosition(BuildBox(1, xs, ys, xe, ye), BuildStore()));
        }

        [Theory]
        [InlineData(0, 0, 11, 8)]
        [InlineData(0, 0, 10, 9)]
        public void ValidateBoxPosition_ShouldThrowArgumentException_WhenBoxExceedsStore(int xs, int ys, int xe, int ye)
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            Assert.Throws<ArgumentException>(() => service.ValidateBoxPosition(BuildBox(1, xs, ys, xe, ye), BuildStore()));
        }

        // --- ValidateStorePlanPosition ---

        [Fact]
        public async Task ValidateStorePlanPosition_ShouldDoNothing_WhenStoreHasNoZone()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var store = BuildStore();
            store.xmin_store = 5;

            await service.ValidateStorePlanPosition(store);
        }

        [Fact]
        public async Task ValidateStorePlanPosition_ShouldDoNothing_WhenStoreHasNoCoordinates()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var store = BuildStore();
            store.id_zone = 1;

            await service.ValidateStorePlanPosition(store);
        }

        [Fact]
        public async Task ValidateStorePlanPosition_ShouldAccept_WhenStoreFitsInZone()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var zone = new Zones { name_zone = "zone", xlength_zone = 20, ylength_zone = 20 };
            context.Zones.Add(zone);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var store = BuildStore();
            store.id_zone = zone.id_zone;
            store.xmin_store = 0;
            store.ymin_store = 0;
            store.xmax_store = 20;
            store.ymax_store = 20;

            await service.ValidateStorePlanPosition(store);
        }

        [Fact]
        public async Task ValidateStorePlanPosition_ShouldThrowArgumentException_WhenOnlySomeCoordinatesAreProvided()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var store = BuildStore();
            store.id_zone = 1;
            store.xmin_store = 0;
            store.ymin_store = 0;

            await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateStorePlanPosition(store));
        }

        [Theory]
        [InlineData(5, 0, 5, 5)]
        [InlineData(0, 5, 5, 5)]
        [InlineData(6, 0, 5, 5)]
        public async Task ValidateStorePlanPosition_ShouldThrowArgumentException_WhenMaxIsNotAfterMin(int xmin, int ymin, int xmax, int ymax)
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var store = BuildStore();
            store.id_zone = 1;
            store.xmin_store = xmin;
            store.ymin_store = ymin;
            store.xmax_store = xmax;
            store.ymax_store = ymax;

            await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateStorePlanPosition(store));
        }

        [Fact]
        public async Task ValidateStorePlanPosition_ShouldThrowKeyNotFoundException_WhenZoneDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var store = BuildStore();
            store.id_zone = 999;
            store.xmin_store = 0;
            store.ymin_store = 0;
            store.xmax_store = 1;
            store.ymax_store = 1;

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ValidateStorePlanPosition(store));
        }

        [Theory]
        [InlineData(21, 10)]
        [InlineData(10, 21)]
        public async Task ValidateStorePlanPosition_ShouldThrowArgumentException_WhenStoreExceedsZone(int xmax, int ymax)
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var zone = new Zones { name_zone = "zone", xlength_zone = 20, ylength_zone = 20 };
            context.Zones.Add(zone);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var store = BuildStore();
            store.id_zone = zone.id_zone;
            store.xmin_store = 0;
            store.ymin_store = 0;
            store.xmax_store = xmax;
            store.ymax_store = ymax;

            await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateStorePlanPosition(store));
        }

        // --- UpdateStoreInformations ---

        [Fact]
        public async Task UpdateStoreInformations_ShouldApplyAllProvidedFields()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var zone = new Zones { name_zone = "zone", xlength_zone = 20, ylength_zone = 20 };
            context.Zones.Add(zone);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var store = BuildStore();

            await service.UpdateStoreInformations(store, new UpdateStoreDto
            {
                name_store = "renamed",
                xlength_store = 3,
                ylength_store = 4,
                position_mode_store = StorePositionMode.Border,
                mqtt_name_store = "renamed-mqtt",
                id_zone = zone.id_zone,
                xmin_store = 1,
                ymin_store = 2,
                xmax_store = 3,
                ymax_store = 4
            });

            Assert.Equal("renamed", store.name_store);
            Assert.Equal(3, store.xlength_store);
            Assert.Equal(4, store.ylength_store);
            Assert.Equal(StorePositionMode.Border, store.position_mode_store);
            Assert.Equal("renamed-mqtt", store.mqtt_name_store);
            Assert.Equal(zone.id_zone, store.id_zone);
            Assert.Equal(1, store.xmin_store);
            Assert.Equal(2, store.ymin_store);
            Assert.Equal(3, store.xmax_store);
            Assert.Equal(4, store.ymax_store);
        }

        [Fact]
        public async Task UpdateStoreInformations_ShouldLeaveStoreUntouched_WhenDtoIsEmpty()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var store = BuildStore();

            await service.UpdateStoreInformations(store, new UpdateStoreDto());

            Assert.Equal("store", store.name_store);
            Assert.Equal(10, store.xlength_store);
            Assert.Null(store.id_zone);
        }

        [Fact]
        public async Task UpdateStoreInformations_ShouldClearZoneAndCoordinates_WhenUnsetRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var store = BuildStore();
            store.id_zone = 1;
            store.xmin_store = 1;
            store.ymin_store = 1;
            store.xmax_store = 2;
            store.ymax_store = 2;

            await service.UpdateStoreInformations(store, new UpdateStoreDto { unset_zone_store = true, id_zone = 5, xmin_store = 9 });

            Assert.Null(store.id_zone);
            Assert.Null(store.xmin_store);
            Assert.Null(store.ymin_store);
            Assert.Null(store.xmax_store);
            Assert.Null(store.ymax_store);
        }

        [Fact]
        public async Task UpdateStoreInformations_ShouldThrowKeyNotFoundException_WhenZoneDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.UpdateStoreInformations(BuildStore(), new UpdateStoreDto { id_zone = 999 }));
        }

        // --- UpdateBoxInformations ---

        [Fact]
        public async Task UpdateBoxInformations_ShouldApplyAllProvidedFields()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var target = BuildStore();
            context.Stores.Add(target);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var box = BuildBox(1, 0, 0, 1, 1);

            await service.UpdateBoxInformations(box, new UpdateBoxDto
            {
                xstart_box = 1,
                ystart_box = 2,
                xend_box = 3,
                yend_box = 4,
                new_id_store = target.id_store
            });

            Assert.Equal(1, box.xstart_box);
            Assert.Equal(2, box.ystart_box);
            Assert.Equal(3, box.xend_box);
            Assert.Equal(4, box.yend_box);
            Assert.Equal(target.id_store, box.id_store);
        }

        [Fact]
        public async Task UpdateBoxInformations_ShouldLeaveBoxUntouched_WhenDtoIsEmpty()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var box = BuildBox(1, 0, 0, 1, 1);

            await service.UpdateBoxInformations(box, new UpdateBoxDto());

            Assert.Equal(1, box.id_store);
            Assert.Equal(1, box.xend_box);
        }

        [Fact]
        public async Task UpdateBoxInformations_ShouldThrowKeyNotFoundException_WhenNewStoreDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.UpdateBoxInformations(BuildBox(1, 0, 0, 1, 1), new UpdateBoxDto { new_id_store = 999 }));
        }

        // --- UpdateLedInformations ---

        [Fact]
        public async Task UpdateLedInformations_ShouldApplyProvidedFields()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var led = new Leds { id_store = 1, x_led = 0, y_led = 0, mqtt_id_led = 1 };

            await service.UpdateLedInformations(led, new UpdateLedDto { x_led = 2, y_led = 3, mqtt_id_led = 4 });

            Assert.Equal(2, led.x_led);
            Assert.Equal(3, led.y_led);
            Assert.Equal(4, led.mqtt_id_led);
        }

        [Fact]
        public async Task UpdateLedInformations_ShouldLeaveLedUntouched_WhenDtoIsEmpty()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var led = new Leds { id_store = 1, x_led = 5, y_led = 6, mqtt_id_led = 7 };

            await service.UpdateLedInformations(led, new UpdateLedDto());

            Assert.Equal(5, led.x_led);
            Assert.Equal(6, led.y_led);
            Assert.Equal(7, led.mqtt_id_led);
        }

        // --- CheckUpdateStoreOutsideElement ---

        [Fact]
        public async Task CheckUpdateStoreOutsideElement_ShouldAccept_WhenElementsStillFit()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            context.Boxs.Add(BuildBox(store.id_store, 0, 0, 10, 8));
            context.Leds.Add(new Leds { id_store = store.id_store, x_led = 9, y_led = 7, mqtt_id_led = 1 });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.CheckUpdateStoreOutsideElement(store);
        }

        [Fact]
        public async Task CheckUpdateStoreOutsideElement_ShouldThrowArgumentException_WhenBoxWouldBeOutside()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            context.Boxs.Add(BuildBox(store.id_store, 0, 0, 10, 8));
            await context.SaveChangesAsync();
            store.xlength_store = 5;
            var service = CreateService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.CheckUpdateStoreOutsideElement(store));
        }

        [Fact]
        public async Task CheckUpdateStoreOutsideElement_ShouldThrowArgumentException_WhenLedWouldBeOutside()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            context.Leds.Add(new Leds { id_store = store.id_store, x_led = 9, y_led = 0, mqtt_id_led = 1 });
            await context.SaveChangesAsync();
            store.xlength_store = 5;
            var service = CreateService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.CheckUpdateStoreOutsideElement(store));
        }

        [Fact]
        public async Task CheckUpdateStoreOutsideElement_ShouldThrowArgumentException_WhenLedIsInvalidForNewBorderMode()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            context.Leds.Add(new Leds { id_store = store.id_store, x_led = 1, y_led = 99, mqtt_id_led = 1 });
            await context.SaveChangesAsync();
            store.position_mode_store = StorePositionMode.Border;
            var service = CreateService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.CheckUpdateStoreOutsideElement(store));
        }

        // --- CheckCreateBoxPositionOverlap ---

        [Fact]
        public async Task CheckCreateBoxPositionOverlap_ShouldAccept_WhenBoxesDoNotOverlap()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            context.Boxs.Add(BuildBox(store.id_store, 0, 0, 2, 2));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.CheckCreateBoxPositionOverlap(new CreateBoxDto { id_store = store.id_store, xstart_box = 2, ystart_box = 0, xend_box = 4, yend_box = 2 });
            await service.CheckCreateBoxPositionOverlap(new CreateBoxDto { id_store = store.id_store, xstart_box = 0, ystart_box = 2, xend_box = 2, yend_box = 4 });
        }

        [Theory]
        [InlineData(1, 1, 3, 3)]
        [InlineData(0, 0, 2, 2)]
        [InlineData(-1, -1, 1, 1)]
        [InlineData(0, 0, 5, 5)]
        public async Task CheckCreateBoxPositionOverlap_ShouldThrowArgumentException_WhenBoxesOverlap(int xs, int ys, int xe, int ye)
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            context.Boxs.Add(BuildBox(store.id_store, 0, 0, 2, 2));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.CheckCreateBoxPositionOverlap(new CreateBoxDto { id_store = store.id_store, xstart_box = xs, ystart_box = ys, xend_box = xe, yend_box = ye }));
        }

        [Fact]
        public async Task CheckCreateBoxPositionOverlap_ShouldIgnoreBoxesOfOtherStores()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            var other = BuildStore();
            context.Stores.AddRange(store, other);
            await context.SaveChangesAsync();
            context.Boxs.Add(BuildBox(other.id_store, 0, 0, 2, 2));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.CheckCreateBoxPositionOverlap(new CreateBoxDto { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 2, yend_box = 2 });
        }

        // --- CheckUpdateBoxPositionOverlap ---

        [Fact]
        public async Task CheckUpdateBoxPositionOverlap_ShouldAccept_WhenOnlyOverlappingItself()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = BuildBox(store.id_store, 0, 0, 2, 2);
            context.Boxs.Add(box);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.CheckUpdateBoxPositionOverlap(box);
        }

        [Fact]
        public async Task CheckUpdateBoxPositionOverlap_ShouldThrowArgumentException_WhenOverlappingAnotherBox()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = BuildBox(store.id_store, 0, 0, 2, 2);
            var neighbour = BuildBox(store.id_store, 3, 0, 5, 2);
            context.Boxs.AddRange(box, neighbour);
            await context.SaveChangesAsync();
            box.xend_box = 4;
            var service = CreateService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.CheckUpdateBoxPositionOverlap(box));
        }

        [Fact]
        public async Task CheckUpdateBoxPositionOverlap_ShouldAccept_WhenBoxesJustTouch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = BuildBox(store.id_store, 0, 0, 3, 2);
            var neighbour = BuildBox(store.id_store, 3, 0, 5, 2);
            context.Boxs.AddRange(box, neighbour);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.CheckUpdateBoxPositionOverlap(box);
        }
    }
}
