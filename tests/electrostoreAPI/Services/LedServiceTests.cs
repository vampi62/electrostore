using Microsoft.EntityFrameworkCore;
using Moq;
using MQTTnet;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        // --- CreateBulkLed ---

        [Fact]
        public async Task CreateBulkLed_ShouldReturnValidAndErrorEntries()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.CreateBulkLed(
            [
                new CreateLedDto { id_store = store.id_store, x_led = 1, y_led = 1, mqtt_id_led = 1 },
                new CreateLedDto { id_store = store.id_store, x_led = 999, y_led = 1, mqtt_id_led = 2 },
                new CreateLedDto { id_store = 999, x_led = 1, y_led = 1, mqtt_id_led = 3 }
            ]);

            Assert.Single(result.valide);
            Assert.Equal(2, result.error.Count);
            Assert.Equal(1, await context.Leds.CountAsync());
        }

        [Fact]
        public async Task CreateBulkLed_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Moderator);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateBulkLed([]));
        }

        // --- UpdateBulkLed ---

        [Fact]
        public async Task UpdateBulkLed_ShouldReturnValidAndErrorEntries()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            var otherStore = BuildStore("other");
            context.Stores.AddRange(store, otherStore);
            await context.SaveChangesAsync();
            var led = new Leds { id_store = store.id_store, x_led = 1, y_led = 1, mqtt_id_led = 1 };
            var foreign = new Leds { id_store = otherStore.id_store, x_led = 1, y_led = 1, mqtt_id_led = 2 };
            context.Leds.AddRange(led, foreign);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateBulkLed(
            [
                new UpdateBulkLedByStoreDto { id_led = led.id_led, x_led = 5 },
                new UpdateBulkLedByStoreDto { id_led = foreign.id_led, x_led = 5 },
                new UpdateBulkLedByStoreDto { id_led = 999, x_led = 5 },
                new UpdateBulkLedByStoreDto { id_led = led.id_led, x_led = 9999 }
            ], store.id_store);

            var updated = Assert.Single(result.valide);
            Assert.Equal(5, updated.x_led);
            Assert.Equal(3, result.error.Count);
        }

        [Fact]
        public async Task UpdateBulkLed_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateBulkLed([], 1));
        }

        // --- DeleteBulkLed ---

        [Fact]
        public async Task DeleteBulkLed_ShouldReturnValidAndErrorEntries()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            var otherStore = BuildStore("other");
            context.Stores.AddRange(store, otherStore);
            await context.SaveChangesAsync();
            var led = new Leds { id_store = store.id_store, x_led = 1, y_led = 1, mqtt_id_led = 1 };
            var foreign = new Leds { id_store = otherStore.id_store, x_led = 1, y_led = 1, mqtt_id_led = 2 };
            context.Leds.AddRange(led, foreign);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.DeleteBulkLed([led.id_led, foreign.id_led, 999], store.id_store);

            var deleted = Assert.Single(result.valide);
            Assert.Equal(led.id_led, deleted.id_led);
            Assert.Equal(2, result.error.Count);
            Assert.Equal(1, await context.Leds.CountAsync());
        }

        [Fact]
        public async Task DeleteBulkLed_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteBulkLed([1], 1));
        }

        // --- ShowLedById ---

        private List<MqttApplicationMessage> CapturePublishedMessages()
        {
            var published = new List<MqttApplicationMessage>();
            _mqttClient.Setup(c => c.IsConnected).Returns(true);
            _mqttClient
                .Setup(c => c.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
                .Callback<MqttApplicationMessage, CancellationToken>((message, _) => published.Add(message))
                .ReturnsAsync((MqttClientPublishResult)null!);
            return published;
        }

        [Fact]
        public async Task ShowLedById_ShouldPublishLedCommandOnStoreTopic()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var led = new Leds { id_store = store.id_store, x_led = 1, y_led = 1, mqtt_id_led = 7 };
            context.Leds.Add(led);
            await context.SaveChangesAsync();
            var published = CapturePublishedMessages();
            var service = CreateService(context);

            await service.ShowLedById(store.id_store, led.id_led, 10, 20, 30, 500, 2);

            var message = Assert.Single(published);
            Assert.Equal("electrostore/store-mqtt/leds", message.Topic);
            var payload = System.Text.Json.JsonDocument.Parse(System.Buffers.BuffersExtensions.ToArray(message.Payload)).RootElement.GetProperty("leds");
            var entry = Assert.Single(payload.EnumerateArray());
            Assert.Equal(7, entry.GetProperty("index").GetInt32());
            Assert.Equal(10, entry.GetProperty("red").GetInt32());
            Assert.Equal(20, entry.GetProperty("green").GetInt32());
            Assert.Equal(30, entry.GetProperty("blue").GetInt32());
            Assert.Equal(2, entry.GetProperty("module").GetInt32());
            Assert.Equal(500, entry.GetProperty("delay").GetInt32());
            Assert.False(message.Retain);
        }

        [Fact]
        public async Task ShowLedById_ShouldThrowKeyNotFoundException_WhenLedIsNotInStore()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            CapturePublishedMessages();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ShowLedById(store.id_store, 999, 0, 0, 0, 0, 0));
        }

        [Fact]
        public async Task ShowLedById_ShouldThrowNotImplementedException_WhenMqttClientIsNotConnected()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var led = new Leds { id_store = store.id_store, x_led = 1, y_led = 1, mqtt_id_led = 7 };
            context.Leds.Add(led);
            await context.SaveChangesAsync();
            _mqttClient.Setup(c => c.IsConnected).Returns(false);
            var service = CreateService(context);

            await Assert.ThrowsAsync<NotImplementedException>(() => service.ShowLedById(store.id_store, led.id_led, 0, 0, 0, 0, 0));
        }

        // --- ShowLedsByBox ---

        private static async Task<(Stores store, Boxs box)> SeedStoreWithBox(ApplicationDbContext context, StorePositionMode mode = StorePositionMode.Grid)
        {
            var store = BuildStore();
            store.position_mode_store = mode;
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = new Boxs { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 3, yend_box = 3 };
            context.Boxs.Add(box);
            await context.SaveChangesAsync();
            return (store, box);
        }

        private static List<int> PublishedIndexes(IEnumerable<MqttApplicationMessage> messages) =>
            messages
                .SelectMany(m => System.Text.Json.JsonDocument.Parse(System.Buffers.BuffersExtensions.ToArray(m.Payload)).RootElement.GetProperty("leds").EnumerateArray().Select(l => l.GetProperty("index").GetInt32()))
                .OrderBy(i => i)
                .ToList();

        [Fact]
        public async Task ShowLedsByBox_ShouldPublishOnlyLedsInsideBox_InGridMode()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (store, box) = await SeedStoreWithBox(context);
            context.Leds.AddRange(
                new Leds { id_store = store.id_store, x_led = 1, y_led = 1, mqtt_id_led = 1 },
                new Leds { id_store = store.id_store, x_led = 3, y_led = 3, mqtt_id_led = 2 },
                new Leds { id_store = store.id_store, x_led = 4, y_led = 1, mqtt_id_led = 3 },
                new Leds { id_store = store.id_store, x_led = 1, y_led = 4, mqtt_id_led = 4 });
            await context.SaveChangesAsync();
            var published = CapturePublishedMessages();
            var service = CreateService(context);

            await service.ShowLedsByBox(store.id_store, box.id_box, 1, 2, 3, 100, 0);

            Assert.Equal([1, 2], PublishedIndexes(published));
            Assert.All(published, m => Assert.Equal("electrostore/store-mqtt/leds", m.Topic));
        }

        [Fact]
        public async Task ShowLedsByBox_ShouldPublishLedsAlongBoxEdges_InBorderMode()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (store, box) = await SeedStoreWithBox(context, StorePositionMode.Border);
            context.Leds.AddRange(
                new Leds { id_store = store.id_store, x_led = 2, y_led = (int)LedBorderSide.Left, mqtt_id_led = 1 },
                new Leds { id_store = store.id_store, x_led = 2, y_led = (int)LedBorderSide.Top, mqtt_id_led = 2 },
                new Leds { id_store = store.id_store, x_led = 9, y_led = (int)LedBorderSide.Right, mqtt_id_led = 3 },
                new Leds { id_store = store.id_store, x_led = 9, y_led = (int)LedBorderSide.Bottom, mqtt_id_led = 4 });
            await context.SaveChangesAsync();
            var published = CapturePublishedMessages();
            var service = CreateService(context);

            await service.ShowLedsByBox(store.id_store, box.id_box, 1, 2, 3, 100, 0);

            Assert.Equal([1, 2], PublishedIndexes(published));
        }

        [Fact]
        public async Task ShowLedsByBox_ShouldSplitLedsIntoMessagesOfTen()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (store, box) = await SeedStoreWithBox(context);
            for (var i = 0; i < 25; i++)
            {
                context.Leds.Add(new Leds { id_store = store.id_store, x_led = i % 4, y_led = i % 4, mqtt_id_led = i + 1 });
            }
            await context.SaveChangesAsync();
            var published = CapturePublishedMessages();
            var service = CreateService(context);

            await service.ShowLedsByBox(store.id_store, box.id_box, 1, 2, 3, 100, 0);

            Assert.Equal(3, published.Count);
            Assert.Equal(25, PublishedIndexes(published).Count);
        }

        [Fact]
        public async Task ShowLedsByBox_ShouldThrowKeyNotFoundException_WhenStoreDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ShowLedsByBox(999, 1, 0, 0, 0, 0, 0));
        }

        [Fact]
        public async Task ShowLedsByBox_ShouldThrowKeyNotFoundException_WhenBoxIsNotInStore()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (store, box) = await SeedStoreWithBox(context);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ShowLedsByBox(store.id_store, box.id_box + 1, 0, 0, 0, 0, 0));
        }

        [Fact]
        public async Task ShowLedsByBox_ShouldThrowKeyNotFoundException_WhenNoLedMatchesBox()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (store, box) = await SeedStoreWithBox(context);
            context.Leds.Add(new Leds { id_store = store.id_store, x_led = 50, y_led = 50, mqtt_id_led = 1 });
            await context.SaveChangesAsync();
            CapturePublishedMessages();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ShowLedsByBox(store.id_store, box.id_box, 0, 0, 0, 0, 0));
        }

        [Fact]
        public async Task ShowLedsByBox_ShouldThrowNotImplementedException_WhenMqttClientIsNotConnected()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (store, box) = await SeedStoreWithBox(context);
            context.Leds.Add(new Leds { id_store = store.id_store, x_led = 1, y_led = 1, mqtt_id_led = 1 });
            await context.SaveChangesAsync();
            _mqttClient.Setup(c => c.IsConnected).Returns(false);
            var service = CreateService(context);

            await Assert.ThrowsAsync<NotImplementedException>(() => service.ShowLedsByBox(store.id_store, box.id_box, 0, 0, 0, 0, 0));
        }

        [Theory]
        [InlineData(10, 1)]
        [InlineData(20, 2)]
        [InlineData(11, 2)]
        public async Task ShowLedsByBox_ShouldNotSendEmptyTrailingMessage_WhenLedCountIsMultipleOfTen(int ledCount, int expectedMessages)
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (store, box) = await SeedStoreWithBox(context);
            for (var i = 0; i < ledCount; i++)
            {
                context.Leds.Add(new Leds { id_store = store.id_store, x_led = i % 4, y_led = i % 4, mqtt_id_led = i + 1 });
            }
            await context.SaveChangesAsync();
            var published = CapturePublishedMessages();
            var service = CreateService(context);

            await service.ShowLedsByBox(store.id_store, box.id_box, 1, 2, 3, 100, 0);

            Assert.Equal(expectedMessages, published.Count);
            Assert.Equal(ledCount, PublishedIndexes(published).Count);
        }
    }
}
