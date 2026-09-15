using Microsoft.EntityFrameworkCore;
using Moq;
using MQTTnet;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.LedService;
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Services.ValidateStoreService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class LedServiceTests : TestBase
    {
        private readonly Mock<IMqttClient> _mqttClient = new();
        private readonly Mock<ISessionService> _sessionService = new();

        public LedServiceTests()
        {
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Admin);
        }

        private LedService CreateService(ApplicationDbContext context) =>
            new(_mapper, context, _mqttClient.Object, _sessionService.Object, new ValidateStoreService(context));

        private static Stores BuildStore(string name = "store", int xlength = 100, int ylength = 100) => new()
        {
            name_store = name,
            mqtt_name_store = name + "-mqtt",
            xlength_store = xlength,
            ylength_store = ylength
        };

        // --- GetLedsByStoreId ---

        [Fact]
        public async Task GetLedsByStoreId_ShouldReturnLedsForStore()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            context.Leds.Add(new Leds { id_store = store.id_store, x_led = 1, y_led = 1, mqtt_id_led = 1 });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetLedsByStoreId(store.id_store);

            Assert.Single(result.data);
        }

        [Fact]
        public async Task GetLedsByStoreId_ShouldThrowKeyNotFoundException_WhenStoreDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetLedsByStoreId(999));
        }

        // --- GetLedById ---

        [Fact]
        public async Task GetLedById_ShouldReturnLed_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var led = new Leds { id_store = store.id_store, x_led = 1, y_led = 1, mqtt_id_led = 1 };
            context.Leds.Add(led);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetLedById(led.id_led);

            Assert.Equal(led.id_led, result.id_led);
        }

        [Fact]
        public async Task GetLedById_ShouldThrowKeyNotFoundException_WhenStoreIdDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var led = new Leds { id_store = store.id_store, x_led = 1, y_led = 1, mqtt_id_led = 1 };
            context.Leds.Add(led);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetLedById(led.id_led, store.id_store + 1));
        }

        [Fact]
        public async Task GetLedById_ShouldThrowKeyNotFoundException_WhenLedDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetLedById(999));
        }

        // --- CreateLed ---

        [Fact]
        public async Task CreateLed_ShouldPersistLed_WhenClientIsAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateLedDto { x_led = 5, y_led = 5, id_store = store.id_store, mqtt_id_led = 1 };

            var result = await service.CreateLed(dto);

            Assert.Equal(5, result.x_led);
            Assert.Equal(1, await context.Leds.CountAsync());
        }

        [Fact]
        public async Task CreateLed_ShouldThrowArgumentException_WhenPositionIsOutOfStoreBounds()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore(xlength: 10, ylength: 10);
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateLedDto { x_led = 50, y_led = 5, id_store = store.id_store, mqtt_id_led = 1 };

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateLed(dto));
        }

        [Fact]
        public async Task CreateLed_ShouldThrowKeyNotFoundException_WhenStoreDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateLedDto { x_led = 5, y_led = 5, id_store = 999, mqtt_id_led = 1 };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateLed(dto));
        }

        [Fact]
        public async Task CreateLed_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);
            var dto = new CreateLedDto { x_led = 5, y_led = 5, id_store = store.id_store, mqtt_id_led = 1 };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateLed(dto));
        }

        // --- UpdateLed ---

        [Fact]
        public async Task UpdateLed_ShouldUpdateProvidedFields_WhenClientIsAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var led = new Leds { id_store = store.id_store, x_led = 1, y_led = 1, mqtt_id_led = 1 };
            context.Leds.Add(led);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new UpdateLedDto { x_led = 9 };

            var result = await service.UpdateLed(led.id_led, dto);

            Assert.Equal(9, result.x_led);
        }

        [Fact]
        public async Task UpdateLed_ShouldThrowArgumentException_WhenNewPositionIsOutOfBounds()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore(xlength: 10, ylength: 10);
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var led = new Leds { id_store = store.id_store, x_led = 1, y_led = 1, mqtt_id_led = 1 };
            context.Leds.Add(led);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new UpdateLedDto { x_led = 500 };

            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateLed(led.id_led, dto));
        }

        [Fact]
        public async Task UpdateLed_ShouldThrowKeyNotFoundException_WhenLedDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateLed(999, new UpdateLedDto()));
        }

        [Fact]
        public async Task UpdateLed_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var led = new Leds { id_store = store.id_store, x_led = 1, y_led = 1, mqtt_id_led = 1 };
            context.Leds.Add(led);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateLed(led.id_led, new UpdateLedDto { x_led = 2 }));
        }

        // --- DeleteLed ---

        [Fact]
        public async Task DeleteLed_ShouldRemoveLed_WhenClientIsAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var led = new Leds { id_store = store.id_store, x_led = 1, y_led = 1, mqtt_id_led = 1 };
            context.Leds.Add(led);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.DeleteLed(led.id_led);

            Assert.Equal(0, await context.Leds.CountAsync());
        }

        [Fact]
        public async Task DeleteLed_ShouldThrowKeyNotFoundException_WhenStoreIdDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var led = new Leds { id_store = store.id_store, x_led = 1, y_led = 1, mqtt_id_led = 1 };
            context.Leds.Add(led);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteLed(led.id_led, store.id_store + 1));
        }

        [Fact]
        public async Task DeleteLed_ShouldThrowKeyNotFoundException_WhenLedDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteLed(999));
        }

        [Fact]
        public async Task DeleteLed_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var led = new Leds { id_store = store.id_store, x_led = 1, y_led = 1, mqtt_id_led = 1 };
            context.Leds.Add(led);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteLed(led.id_led));
            Assert.Equal(1, await context.Leds.CountAsync());
        }
    }
}
