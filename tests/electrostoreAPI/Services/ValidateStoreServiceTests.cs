using Microsoft.EntityFrameworkCore;
using Xunit;
using System;
using System.Threading.Tasks;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.ValidateStoreService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class ValidateStoreServiceTests : TestBase
    {
        private static ValidateStoreService CreateService(ApplicationDbContext context)
        {
            return new ValidateStoreService(context);
        }

        private static Stores BuildStore(int id, int xLength = 100, int yLength = 100)
        {
            return new Stores
            {
                id_store = id,
                nom_store = "Store",
                xlength_store = xLength,
                ylength_store = yLength,
                mqtt_name_store = "mqtt-" + id
            };
        }

        private static Boxs BuildBox(int id, int storeId, int x = 0, int y = 0, int xEnd = 10, int yEnd = 10)
        {
            return new Boxs
            {
                id_box = id,
                id_store = storeId,
                xstart_box = x,
                ystart_box = y,
                xend_box = xEnd,
                yend_box = yEnd
            };
        }

        private static Leds BuildLed(int id, int storeId, int x = 0, int y = 0)
        {
            return new Leds
            {
                id_led = id,
                id_store = storeId,
                x_led = x,
                y_led = y
            };
        }

        // --- ValidateLedPosition ---

        [Fact]
        public void ValidateLedPosition_ShouldThrowArgumentException_WhenXOutOfBounds()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var store = BuildStore(1, xLength: 50);
            var led = BuildLed(1, 1, x: 50);
            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.ValidateLedPosition(led, store));
        }

        [Fact]
        public void ValidateLedPosition_ShouldThrowArgumentException_WhenYOutOfBounds()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var store = BuildStore(1, yLength: 50);
            var led = BuildLed(1, 1, y: 50);
            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.ValidateLedPosition(led, store));
        }

        [Fact]
        public void ValidateLedPosition_ShouldNotThrow_WhenWithinBounds()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var store = BuildStore(1);
            var led = BuildLed(1, 1, x: 10, y: 10);
            // Act
            var exception = Record.Exception(() => service.ValidateLedPosition(led, store));
            // Assert
            Assert.Null(exception);
        }

        // --- ValidateBoxPosition ---

        [Fact]
        public void ValidateBoxPosition_ShouldThrowArgumentException_WhenStartIsGreaterThanOrEqualEnd()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var store = BuildStore(1);
            var box = BuildBox(1, 1, x: 10, xEnd: 10);
            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.ValidateBoxPosition(box, store));
        }

        [Fact]
        public void ValidateBoxPosition_ShouldThrowArgumentException_WhenPositionExceedsStoreBounds()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var store = BuildStore(1, xLength: 20, yLength: 20);
            var box = BuildBox(1, 1, x: 15, xEnd: 25, y: 0, yEnd: 10);
            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.ValidateBoxPosition(box, store));
        }

        [Fact]
        public void ValidateBoxPosition_ShouldNotThrow_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var store = BuildStore(1);
            var box = BuildBox(1, 1);
            // Act
            var exception = Record.Exception(() => service.ValidateBoxPosition(box, store));
            // Assert
            Assert.Null(exception);
        }

        // --- UpdateStoreInformations ---

        [Fact]
        public async Task UpdateStoreInformations_ShouldUpdateOnlyProvidedFields()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var store = BuildStore(1);
            var dto = new UpdateStoreDto { nom_store = "New name", xlength_store = 200 };
            // Act
            await service.UpdateStoreInformations(store, dto);
            // Assert
            Assert.Equal("New name", store.nom_store);
            Assert.Equal(200, store.xlength_store);
            Assert.Equal(100, store.ylength_store);
        }

        [Fact]
        public async Task UpdateStoreInformations_ShouldLeaveFieldsUnchanged_WhenAllNull()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var store = BuildStore(1);
            var dto = new UpdateStoreDto();
            // Act
            await service.UpdateStoreInformations(store, dto);
            // Assert
            Assert.Equal("Store", store.nom_store);
            Assert.Equal(100, store.xlength_store);
            Assert.Equal(100, store.ylength_store);
        }

        // --- UpdateBoxInformations ---

        [Fact]
        public async Task UpdateBoxInformations_ShouldUpdatePositionFields_WhenProvided()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var box = BuildBox(1, 1);
            var dto = new UpdateBoxDto { xstart_box = 5, ystart_box = 5, xend_box = 15, yend_box = 15 };
            // Act
            await service.UpdateBoxInformations(box, dto);
            // Assert
            Assert.Equal(5, box.xstart_box);
            Assert.Equal(5, box.ystart_box);
            Assert.Equal(15, box.xend_box);
            Assert.Equal(15, box.yend_box);
        }

        [Fact]
        public async Task UpdateBoxInformations_ShouldThrowKeyNotFoundException_WhenNewStoreNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var box = BuildBox(1, 1);
            var dto = new UpdateBoxDto { new_id_store = 999 };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateBoxInformations(box, dto);
            });
        }

        [Fact]
        public async Task UpdateBoxInformations_ShouldUpdateStoreId_WhenNewStoreExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(2));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var box = BuildBox(1, 1);
            var dto = new UpdateBoxDto { new_id_store = 2 };
            // Act
            await service.UpdateBoxInformations(box, dto);
            // Assert
            Assert.Equal(2, box.id_store);
        }

        // --- UpdateLedInformations ---

        [Fact]
        public async Task UpdateLedInformations_ShouldUpdateProvidedFields()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var led = BuildLed(1, 1);
            var dto = new UpdateLedDto { x_led = 20, mqtt_led_id = 5 };
            // Act
            await service.UpdateLedInformations(led, dto);
            // Assert
            Assert.Equal(20, led.x_led);
            Assert.Equal(0, led.y_led);
            Assert.Equal(5, led.mqtt_led_id);
        }

        // --- CheckUpdateStoreOutsideElement ---

        [Fact]
        public async Task CheckUpdateStoreOutsideElement_ShouldThrowArgumentException_WhenBoxOutsideNewBounds()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore(1, xLength: 20, yLength: 20);
            context.Stores.Add(store);
            context.Boxs.Add(BuildBox(1, 1, x: 15, xEnd: 25));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            store.xlength_store = 10;
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.CheckUpdateStoreOutsideElement(store);
            });
        }

        [Fact]
        public async Task CheckUpdateStoreOutsideElement_ShouldThrowArgumentException_WhenLedOutsideNewBounds()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore(1, xLength: 20, yLength: 20);
            context.Stores.Add(store);
            context.Leds.Add(BuildLed(1, 1, x: 15));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            store.xlength_store = 10;
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.CheckUpdateStoreOutsideElement(store);
            });
        }

        [Fact]
        public async Task CheckUpdateStoreOutsideElement_ShouldNotThrow_WhenAllWithinBounds()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore(1);
            context.Stores.Add(store);
            context.Boxs.Add(BuildBox(1, 1));
            context.Leds.Add(BuildLed(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var exception = await Record.ExceptionAsync(async () =>
            {
                await service.CheckUpdateStoreOutsideElement(store);
            });
            // Assert
            Assert.Null(exception);
        }

        // --- CheckCreateBoxPositionOverlap ---

        [Fact]
        public async Task CheckCreateBoxPositionOverlap_ShouldThrowArgumentException_WhenOverlapExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1, x: 0, y: 0, xEnd: 10, yEnd: 10));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var newBox = new CreateBoxDto { xstart_box = 5, ystart_box = 5, xend_box = 15, yend_box = 15, id_store = 1 };
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.CheckCreateBoxPositionOverlap(newBox);
            });
        }

        [Fact]
        public async Task CheckCreateBoxPositionOverlap_ShouldNotThrow_WhenNoOverlap()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1, x: 0, y: 0, xEnd: 10, yEnd: 10));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var newBox = new CreateBoxDto { xstart_box = 20, ystart_box = 20, xend_box = 30, yend_box = 30, id_store = 1 };
            // Act
            var exception = await Record.ExceptionAsync(async () =>
            {
                await service.CheckCreateBoxPositionOverlap(newBox);
            });
            // Assert
            Assert.Null(exception);
        }

        // --- CheckUpdateBoxPositionOverlap ---

        [Fact]
        public async Task CheckUpdateBoxPositionOverlap_ShouldThrowArgumentException_WhenOverlapWithOtherBox()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1, x: 0, y: 0, xEnd: 10, yEnd: 10));
            var boxToUpdate = BuildBox(2, 1, x: 20, y: 20, xEnd: 30, yEnd: 30);
            context.Boxs.Add(boxToUpdate);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            boxToUpdate.xstart_box = 5;
            boxToUpdate.ystart_box = 5;
            boxToUpdate.xend_box = 15;
            boxToUpdate.yend_box = 15;
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.CheckUpdateBoxPositionOverlap(boxToUpdate);
            });
        }

        [Fact]
        public async Task CheckUpdateBoxPositionOverlap_ShouldNotThrow_WhenOnlyOverlapsWithItself()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            var boxToUpdate = BuildBox(1, 1, x: 0, y: 0, xEnd: 10, yEnd: 10);
            context.Boxs.Add(boxToUpdate);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var exception = await Record.ExceptionAsync(async () =>
            {
                await service.CheckUpdateBoxPositionOverlap(boxToUpdate);
            });
            // Assert
            Assert.Null(exception);
        }
    }
}
