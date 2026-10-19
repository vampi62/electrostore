using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Text.Json;
using Xunit;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Kafka.Messages;
using ElectrostoreAPI.Kafka.Producer;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.EncryptionService;
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Services.StoreService;
using ElectrostoreAPI.Services.ValidateStoreService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class StoreServiceTests : TestBase
    {
        private const string MqttTopic = "mqtt-user-events";

        private readonly Mock<ISessionService> _sessionService = new();
        private readonly Mock<IEncryptionService> _encryptionService = new();
        private readonly Mock<IValidateStoreService> _validateStoreService = new();
        private readonly Mock<IKafkaProducerService> _kafkaProducerService = new();
        private readonly IConfiguration _configuration;

        public StoreServiceTests()
        {
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Admin);
            _encryptionService
                .Setup(e => e.Encrypt(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new EncryptDto { encrypted_data = [1, 2, 3], iv = [4, 5], tag = [6, 7] });
            _encryptionService
                .Setup(e => e.Decrypt(It.IsAny<EncryptDto>(), It.IsAny<string>()))
                .ReturnsAsync("decrypted-password");
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Encryption:HexKey"] = "00112233" })
                .Build();
        }

        private StoreService CreateService(ApplicationDbContext context, IConfiguration? configuration = null) =>
            new(_mapper, context, configuration ?? _configuration, _encryptionService.Object, _sessionService.Object,
                _validateStoreService.Object, _kafkaProducerService.Object);

        // The *Complete methods open a transaction, which the in-memory provider only tolerates when its warning is ignored.
        private ApplicationDbContext CreateTransactionalContext() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>(_dbContextOptions)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static Stores BuildStore(string name = "store", string? mqttName = null) => new()
        {
            name_store = name,
            mqtt_name_store = mqttName ?? name + "-mqtt",
            xlength_store = 10,
            ylength_store = 10
        };

        private static CreateStoreDto BuildCreateDto(string name = "new-store", int? idZone = null) => new()
        {
            name_store = name,
            xlength_store = 10,
            ylength_store = 10,
            mqtt_name_store = name + "-mqtt",
            id_zone = idZone
        };

        private IEnumerable<MqttUserMessage> PublishedMessages() =>
            _kafkaProducerService.Invocations
                .Where(i => i.Method.Name == nameof(IKafkaProducerService.PublishAsync))
                .Select(i => JsonSerializer.Deserialize<MqttUserMessage>((string)i.Arguments[2]!)!);

        // --- GetStores ---

        [Fact]
        public async Task GetStores_ShouldReturnStoresWithoutPassword()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.AddRange(BuildStore("a"), BuildStore("b"));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetStores();

            Assert.Equal(2, result.pagination.total);
            Assert.All(result.data, s => Assert.Equal(string.Empty, s.mqtt_password_store));
        }

        [Fact]
        public async Task GetStores_ShouldRestrictToIds_WhenIdResearchProvided()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var a = BuildStore("a");
            var b = BuildStore("b");
            context.Stores.AddRange(a, b);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetStores(idResearch: [b.id_store]);

            var single = Assert.Single(result.data);
            Assert.Equal(b.id_store, single.id_store);
        }

        [Fact]
        public async Task GetStores_ShouldReturnCountsAndExpandedRelations()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var zone = new Zones { name_zone = "zone" };
            var tag = new Tags { name_tag = "tag" };
            context.Zones.Add(zone);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            var store = BuildStore();
            store.id_zone = zone.id_zone;
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            context.Boxs.Add(new Boxs { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 1, yend_box = 1 });
            context.Leds.Add(new Leds { id_store = store.id_store, x_led = 0, y_led = 0, mqtt_id_led = 1 });
            context.StoresTags.Add(new StoresTags { id_store = store.id_store, id_tag = tag.id_tag });
            await context.SaveChangesAsync();
            using var queryContext = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(queryContext);

            var result = await service.GetStores(expand: ["boxs", "leds", "stores_tags", "zone"]);

            var single = Assert.Single(result.data);
            Assert.Equal(1, single.boxs_count);
            Assert.Equal(1, single.leds_count);
            Assert.Equal(1, single.stores_tags_count);
            Assert.Single(single.boxs!);
            Assert.Single(single.leds!);
            Assert.Single(single.stores_tags!);
            Assert.NotNull(single.zone);
        }

        // --- GetStoreById ---

        [Fact]
        public async Task GetStoreById_ShouldReturnDecryptedPassword_WhenClientIsAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetStoreById(store.id_store);

            Assert.Equal("decrypted-password", result.mqtt_password_store);
            Assert.Equal(0, result.boxs_count);
        }

        [Theory]
        [InlineData(UserRole.User)]
        [InlineData(UserRole.Moderator)]
        public async Task GetStoreById_ShouldHidePassword_WhenClientIsNotAdmin(UserRole role)
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(role);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetStoreById(store.id_store);

            Assert.Equal(string.Empty, result.mqtt_password_store);
            _encryptionService.Verify(e => e.Decrypt(It.IsAny<EncryptDto>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetStoreById_ShouldReturnExpandedRelations()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var zone = new Zones { name_zone = "zone" };
            context.Zones.Add(zone);
            await context.SaveChangesAsync();
            var store = BuildStore();
            store.id_zone = zone.id_zone;
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            context.Boxs.Add(new Boxs { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 1, yend_box = 1 });
            context.Leds.Add(new Leds { id_store = store.id_store, x_led = 0, y_led = 0, mqtt_id_led = 1 });
            await context.SaveChangesAsync();
            using var queryContext = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(queryContext);

            var result = await service.GetStoreById(store.id_store, ["boxs", "leds", "stores_tags", "zone"]);

            Assert.Single(result.boxs!);
            Assert.Single(result.leds!);
            Assert.Empty(result.stores_tags!);
            Assert.NotNull(result.zone);
        }

        [Fact]
        public async Task GetStoreById_ShouldThrowKeyNotFoundException_WhenStoreDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetStoreById(999));
        }

        // --- Constructor ---

        [Fact]
        public void Constructor_ShouldThrowInvalidOperationException_WhenEncryptionKeyIsMissing()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var emptyConfiguration = new ConfigurationBuilder().Build();

            Assert.Throws<InvalidOperationException>(() => CreateService(context, emptyConfiguration));
        }

        // --- CreateStore ---

        [Fact]
        public async Task CreateStore_ShouldPersistStoreEncryptPasswordAndPublishToKafka()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            var result = await service.CreateStore(BuildCreateDto());

            Assert.Equal("new-store", result.name_store);
            Assert.False(string.IsNullOrEmpty(result.mqtt_password_store));
            var stored = await context.Stores.SingleAsync();
            Assert.Equal(new byte[] { 1, 2, 3 }, stored.mqtt_password_store);
            Assert.Equal(new byte[] { 4, 5 }, stored.mqtt_password_encryption_iv_store);
            Assert.Equal(new byte[] { 6, 7 }, stored.mqtt_password_encryption_tag_store);
            _validateStoreService.Verify(v => v.ValidateStorePlanPosition(It.IsAny<Stores>()), Times.Once);
            _kafkaProducerService.Verify(k => k.PublishAsync(MqttTopic, stored.id_store.ToString(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            var message = Assert.Single(PublishedMessages());
            Assert.Equal("new-store-mqtt", message.user);
            Assert.Equal(result.mqtt_password_store, message.password);
            Assert.False(message.delete);
        }

        [Fact]
        public async Task CreateStore_ShouldKeepZoneId_WhenZoneExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var zone = new Zones { name_zone = "zone" };
            context.Zones.Add(zone);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.CreateStore(BuildCreateDto(idZone: zone.id_zone));

            Assert.Equal(zone.id_zone, result.id_zone);
        }

        [Fact]
        public async Task CreateStore_ShouldThrowKeyNotFoundException_WhenZoneDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateStore(BuildCreateDto(idZone: 999)));
            _kafkaProducerService.Verify(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(UserRole.User)]
        [InlineData(UserRole.Moderator)]
        public async Task CreateStore_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin(UserRole role)
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(role);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateStore(BuildCreateDto()));
            Assert.Equal(0, await context.Stores.CountAsync());
        }

        // --- UpdateStore ---

        [Fact]
        public async Task UpdateStore_ShouldReturnExistingDecryptedPassword_WhenPasswordIsNotReset()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateStore(store.id_store, new UpdateStoreDto { name_store = "renamed" });

            Assert.Equal("decrypted-password", result.mqtt_password_store);
            _validateStoreService.Verify(v => v.UpdateStoreInformations(It.IsAny<Stores>(), It.IsAny<UpdateStoreDto>()), Times.Once);
            _validateStoreService.Verify(v => v.ValidateStorePlanPosition(It.IsAny<Stores>()), Times.Once);
            _validateStoreService.Verify(v => v.CheckUpdateStoreOutsideElement(It.IsAny<Stores>()), Times.Once);
            _encryptionService.Verify(e => e.Encrypt(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _kafkaProducerService.Verify(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateStore_ShouldGenerateNewPasswordAndPublish_WhenResetRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore(mqttName: "old-mqtt");
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateStore(store.id_store, new UpdateStoreDto { reset_mqtt_password_store = true });

            Assert.False(string.IsNullOrEmpty(result.mqtt_password_store));
            Assert.NotEqual("decrypted-password", result.mqtt_password_store);
            var message = Assert.Single(PublishedMessages());
            Assert.Equal("old-mqtt", message.old_user);
            Assert.Equal(result.mqtt_password_store, message.password);
            Assert.False(message.delete);
        }

        [Fact]
        public async Task UpdateStore_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Moderator);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateStore(1, new UpdateStoreDto()));
        }

        [Fact]
        public async Task UpdateStore_ShouldThrowKeyNotFoundException_WhenStoreDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateStore(999, new UpdateStoreDto()));
        }

        // --- DeleteStore ---

        [Fact]
        public async Task DeleteStore_ShouldRemoveStoreAndPublishDeleteMessage()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore(mqttName: "to-delete");
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.DeleteStore(store.id_store);

            Assert.Equal(0, await context.Stores.CountAsync());
            var message = Assert.Single(PublishedMessages());
            Assert.Equal("to-delete", message.user);
            Assert.True(message.delete);
        }

        [Fact]
        public async Task DeleteStore_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteStore(1));
        }

        [Fact]
        public async Task DeleteStore_ShouldThrowKeyNotFoundException_WhenStoreDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteStore(999));
        }

        // --- UpdateStoreMqttStatusByMqttNameAsync ---

        [Fact]
        public async Task UpdateStoreMqttStatus_ShouldUpdateAllMatchingStores()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.AddRange(BuildStore("a", "shared"), BuildStore("b", "shared"), BuildStore("c", "other"));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var count = await service.UpdateStoreMqttStatusByMqttNameAsync("shared", new UpdateStoreMqttStatusDto { is_mqtt_connected_store = true }, CancellationToken.None);

            Assert.Equal(2, count);
            var shared = await context.Stores.Where(s => s.mqtt_name_store == "shared").ToListAsync();
            Assert.All(shared, s =>
            {
                Assert.True(s.is_mqtt_connected_store);
                Assert.NotNull(s.mqtt_last_seen_store);
            });
            var other = await context.Stores.SingleAsync(s => s.mqtt_name_store == "other");
            Assert.False(other.is_mqtt_connected_store);
        }

        [Fact]
        public async Task UpdateStoreMqttStatus_ShouldReturnZero_WhenNoStoreMatches()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            var count = await service.UpdateStoreMqttStatusByMqttNameAsync("unknown", new UpdateStoreMqttStatusDto { is_mqtt_connected_store = true }, CancellationToken.None);

            Assert.Equal(0, count);
        }

        // --- CreateStoreComplete ---

        [Fact]
        public async Task CreateStoreComplete_ShouldPersistStoreWithLedsAndBoxs_AndPublish()
        {
            using var context = CreateTransactionalContext();
            var service = CreateService(context);

            var result = await service.CreateStoreComplete(new CreateStoreCompleteDto
            {
                store = BuildCreateDto(),
                leds = [new CreateLedByStoreDto { x_led = 1, y_led = 1, mqtt_id_led = 7 }],
                boxs = [new CreateBoxByStoreDto { xstart_box = 0, ystart_box = 0, xend_box = 2, yend_box = 2 }]
            });

            Assert.Single(result.leds!.valide);
            Assert.Empty(result.leds.error);
            Assert.Single(result.boxs!.valide);
            Assert.Empty(result.boxs.error);
            Assert.False(string.IsNullOrEmpty(result.store.mqtt_password_store));
            Assert.Equal(1, await context.Stores.CountAsync());
            Assert.Equal(1, await context.Leds.CountAsync());
            Assert.Equal(1, await context.Boxs.CountAsync());
            var message = Assert.Single(PublishedMessages());
            Assert.Equal("new-store-mqtt", message.user);
        }

        [Fact]
        public async Task CreateStoreComplete_ShouldWork_WhenLedsAndBoxsAreOmitted()
        {
            using var context = CreateTransactionalContext();
            var service = CreateService(context);

            var result = await service.CreateStoreComplete(new CreateStoreCompleteDto { store = BuildCreateDto() });

            Assert.Empty(result.leds!.valide);
            Assert.Empty(result.boxs!.valide);
            Assert.Single(PublishedMessages());
        }

        [Fact]
        public async Task CreateStoreComplete_ShouldReportErrorsAndSkipPublish_WhenAnElementIsInvalid()
        {
            using var context = CreateTransactionalContext();
            _validateStoreService
                .Setup(v => v.ValidateLedPosition(It.IsAny<Leds>(), It.IsAny<Stores>()))
                .Throws(new ArgumentException("led out of store"));
            _validateStoreService
                .Setup(v => v.CheckCreateBoxPositionOverlap(It.IsAny<CreateBoxDto>()))
                .ThrowsAsync(new InvalidOperationException("overlap"));
            var service = CreateService(context);

            var result = await service.CreateStoreComplete(new CreateStoreCompleteDto
            {
                store = BuildCreateDto(),
                leds = [new CreateLedByStoreDto { x_led = 1, y_led = 1, mqtt_id_led = 7 }],
                boxs = [new CreateBoxByStoreDto { xstart_box = 0, ystart_box = 0, xend_box = 2, yend_box = 2 }]
            });

            var ledError = Assert.Single(result.leds!.error);
            Assert.Equal("led out of store", ledError.reason);
            var boxError = Assert.Single(result.boxs!.error);
            Assert.Equal("overlap", boxError.reason);
            Assert.Empty(PublishedMessages());
        }

        [Fact]
        public async Task CreateStoreComplete_ShouldThrowKeyNotFoundException_WhenZoneDoesNotExist()
        {
            using var context = CreateTransactionalContext();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.CreateStoreComplete(new CreateStoreCompleteDto { store = BuildCreateDto(idZone: 999) }));
        }

        [Fact]
        public async Task CreateStoreComplete_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = CreateTransactionalContext();
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.CreateStoreComplete(new CreateStoreCompleteDto { store = BuildCreateDto() }));
        }

        // --- UpdateStoreComplete ---

        private async Task<(Stores store, Boxs box, Leds led)> SeedStoreWithElements(ApplicationDbContext context)
        {
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = new Boxs { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 1, yend_box = 1 };
            var led = new Leds { id_store = store.id_store, x_led = 0, y_led = 0, mqtt_id_led = 1 };
            context.Boxs.Add(box);
            context.Leds.Add(led);
            await context.SaveChangesAsync();
            return (store, box, led);
        }

        [Fact]
        public async Task UpdateStoreComplete_ShouldApplyNewModifiedAndDeletedElements()
        {
            using var context = CreateTransactionalContext();
            var (store, box, led) = await SeedStoreWithElements(context);
            var extraBox = new Boxs { id_store = store.id_store, xstart_box = 5, ystart_box = 5, xend_box = 6, yend_box = 6 };
            var extraLed = new Leds { id_store = store.id_store, x_led = 5, y_led = 5, mqtt_id_led = 2 };
            context.Boxs.Add(extraBox);
            context.Leds.Add(extraLed);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateStoreComplete(store.id_store, new UpdateStoreCompleteDto
            {
                store = new UpdateStoreDto { name_store = "renamed" },
                leds =
                [
                    new UpdateBulkLedByStoreDto { id_led = led.id_led, x_led = 3, status = "modified" },
                    new UpdateBulkLedByStoreDto { id_led = extraLed.id_led, status = "delete" },
                    new UpdateBulkLedByStoreDto { id_led = 0, x_led = 4, y_led = 4, mqtt_id_led = 9, status = "new" }
                ],
                boxs =
                [
                    new UpdateBulkBoxByStoreDto { id_box = box.id_box, xend_box = 3, status = "modified" },
                    new UpdateBulkBoxByStoreDto { id_box = extraBox.id_box, status = "delete" }
                ]
            });

            Assert.Equal(2, result.leds!.valide.Count());
            Assert.Empty(result.leds.error);
            Assert.Single(result.boxs!.valide);
            Assert.Empty(result.boxs.error);
            Assert.Equal(2, await context.Leds.CountAsync());
            Assert.Equal(1, await context.Boxs.CountAsync());
            Assert.Equal("decrypted-password", result.store.mqtt_password_store);
            _validateStoreService.Verify(v => v.CheckUpdateStoreOutsideElement(It.IsAny<Stores>()), Times.Once);
            Assert.Empty(PublishedMessages());
        }

        [Fact]
        public async Task UpdateStoreComplete_ShouldResetPasswordAndPublish_WhenRequested()
        {
            using var context = CreateTransactionalContext();
            var (store, _, _) = await SeedStoreWithElements(context);
            var service = CreateService(context);

            var result = await service.UpdateStoreComplete(store.id_store, new UpdateStoreCompleteDto
            {
                store = new UpdateStoreDto { reset_mqtt_password_store = true }
            });

            Assert.False(string.IsNullOrEmpty(result.store.mqtt_password_store));
            Assert.NotEqual("decrypted-password", result.store.mqtt_password_store);
            var message = Assert.Single(PublishedMessages());
            Assert.Equal(result.store.mqtt_password_store, message.password);
            Assert.Equal("store-mqtt", message.old_user);
        }

        [Fact]
        public async Task UpdateStoreComplete_ShouldReportErrors_WhenElementsAreInvalid()
        {
            using var context = CreateTransactionalContext();
            var (store, _, _) = await SeedStoreWithElements(context);
            var otherStore = BuildStore("other");
            context.Stores.Add(otherStore);
            await context.SaveChangesAsync();
            var foreignLed = new Leds { id_store = otherStore.id_store, x_led = 0, y_led = 0, mqtt_id_led = 3 };
            var foreignBox = new Boxs { id_store = otherStore.id_store, xstart_box = 0, ystart_box = 0, xend_box = 1, yend_box = 1 };
            context.Leds.Add(foreignLed);
            context.Boxs.Add(foreignBox);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateStoreComplete(store.id_store, new UpdateStoreCompleteDto
            {
                store = new UpdateStoreDto(),
                leds =
                [
                    new UpdateBulkLedByStoreDto { id_led = 999, status = "delete" },
                    new UpdateBulkLedByStoreDto { id_led = foreignLed.id_led, status = "delete" },
                    new UpdateBulkLedByStoreDto { id_led = 999, status = "modified" },
                    new UpdateBulkLedByStoreDto { id_led = foreignLed.id_led, status = "modified" },
                    new UpdateBulkLedByStoreDto { id_led = 0, status = "new" }
                ],
                boxs =
                [
                    new UpdateBulkBoxByStoreDto { id_box = 999, status = "delete" },
                    new UpdateBulkBoxByStoreDto { id_box = foreignBox.id_box, status = "delete" },
                    new UpdateBulkBoxByStoreDto { id_box = 999, status = "modified" },
                    new UpdateBulkBoxByStoreDto { id_box = foreignBox.id_box, status = "modified" },
                    new UpdateBulkBoxByStoreDto { id_box = 0, status = "new" }
                ]
            });

            Assert.Equal(5, result.leds!.error.Count());
            Assert.Equal(5, result.boxs!.error.Count());
            Assert.Empty(result.leds.valide);
            Assert.Empty(result.boxs.valide);
        }

        [Fact]
        public async Task UpdateStoreComplete_ShouldReportBoxOverlapErrors_WhenCheckFails()
        {
            using var context = CreateTransactionalContext();
            var (store, box, _) = await SeedStoreWithElements(context);
            _validateStoreService
                .Setup(v => v.CheckUpdateBoxPositionOverlap(It.IsAny<Boxs>()))
                .ThrowsAsync(new InvalidOperationException("overlap"));
            var service = CreateService(context);

            var result = await service.UpdateStoreComplete(store.id_store, new UpdateStoreCompleteDto
            {
                store = new UpdateStoreDto { reset_mqtt_password_store = true },
                boxs = [new UpdateBulkBoxByStoreDto { id_box = box.id_box, xend_box = 3, status = "modified" }]
            });

            var error = Assert.Single(result.boxs!.error);
            Assert.Equal("overlap", error.reason);
            Assert.Empty(PublishedMessages());
        }

        [Fact]
        public async Task UpdateStoreComplete_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = CreateTransactionalContext();
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Moderator);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.UpdateStoreComplete(1, new UpdateStoreCompleteDto { store = new UpdateStoreDto() }));
        }

        [Fact]
        public async Task UpdateStoreComplete_ShouldThrowKeyNotFoundException_WhenStoreDoesNotExist()
        {
            using var context = CreateTransactionalContext();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.UpdateStoreComplete(999, new UpdateStoreCompleteDto { store = new UpdateStoreDto() }));
        }
    }
}
