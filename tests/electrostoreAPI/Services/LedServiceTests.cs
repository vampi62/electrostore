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
        private readonly Mock<IMqttClient> _mqttClient;
        private readonly Mock<ISessionService> _sessionService;
        private readonly Mock<IValidateStoreService> _validateStoreService;

        public LedServiceTests()
        {
            _mqttClient = new Mock<IMqttClient>();
            _sessionService = new Mock<ISessionService>();
            _validateStoreService = new Mock<IValidateStoreService>();
        }

        private LedService CreateService(ApplicationDbContext context)
        {
            return new LedService(_mapper, context, _mqttClient.Object, _sessionService.Object, _validateStoreService.Object);
        }

        private void SetClientRole(UserRole role)
        {
            _sessionService.Setup(s => s.GetClientRole()).Returns(role);
        }

        private static Stores BuildStore(int id, string name = "Store")
        {
            return new Stores
            {
                id_store = id,
                nom_store = name,
                xlength_store = 100,
                ylength_store = 100,
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

        private static Leds BuildLed(int id, int storeId, int x = 0, int y = 0, int mqttId = 0)
        {
            return new Leds
            {
                id_led = id,
                id_store = storeId,
                x_led = x,
                y_led = y,
                mqtt_led_id = mqttId
            };
        }

        // --- GetLedsByStoreId ---

        [Fact]
        public async Task GetLedsByStoreId_ShouldThrowKeyNotFoundException_WhenStoreNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetLedsByStoreId(1);
            });
        }

        [Fact]
        public async Task GetLedsByStoreId_ShouldReturnOnlyLedsForGivenStore()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Stores.Add(BuildStore(2));
            context.Leds.Add(BuildLed(1, 1));
            context.Leds.Add(BuildLed(2, 1));
            context.Leds.Add(BuildLed(3, 2));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetLedsByStoreId(1);
            // Assert
            Assert.Equal(2, result.data.Count());
            Assert.Equal(2, result.pagination.total);
        }

        // --- GetLedById ---

        [Fact]
        public async Task GetLedById_ShouldReturnLed_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Leds.Add(BuildLed(1, 1, mqttId: 5));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetLedById(1);
            // Assert
            Assert.Equal(5, result.mqtt_led_id);
        }

        [Fact]
        public async Task GetLedById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetLedById(999);
            });
        }

        [Fact]
        public async Task GetLedById_ShouldThrowKeyNotFoundException_WhenStoreIdMismatch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Stores.Add(BuildStore(2));
            context.Leds.Add(BuildLed(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetLedById(1, storeId: 2);
            });
        }

        // --- CreateLed ---

        [Fact]
        public async Task CreateLed_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.User);
            var service = CreateService(context);
            var dto = new CreateLedDto { x_led = 1, y_led = 1, id_store = 1, mqtt_led_id = 1 };
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.CreateLed(dto);
            });
        }

        [Fact]
        public async Task CreateLed_ShouldThrowKeyNotFoundException_WhenStoreNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            var dto = new CreateLedDto { x_led = 1, y_led = 1, id_store = 999, mqtt_led_id = 1 };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreateLed(dto);
            });
        }

        [Fact]
        public async Task CreateLed_ShouldCreateLed_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            var dto = new CreateLedDto { x_led = 1, y_led = 1, id_store = 1, mqtt_led_id = 1 };
            // Act
            var result = await service.CreateLed(dto);
            // Assert
            Assert.Equal(1, result.id_store);
            Assert.Equal(1, await context.Leds.CountAsync());
            _validateStoreService.Verify(v => v.ValidateLedPosition(It.IsAny<Leds>(), It.IsAny<Stores>()), Times.Once);
        }

        // --- CreateBulkLed ---

        [Fact]
        public async Task CreateBulkLed_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.User);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.CreateBulkLed(new List<CreateLedDto>());
            });
        }

        [Fact]
        public async Task CreateBulkLed_ShouldReturnMixedResults_WhenOneStoreMissing()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            var dtos = new List<CreateLedDto>
            {
                new() { x_led = 1, y_led = 1, id_store = 1, mqtt_led_id = 1 },
                new() { x_led = 1, y_led = 1, id_store = 999, mqtt_led_id = 2 }
            };
            // Act
            var result = await service.CreateBulkLed(dtos);
            // Assert
            Assert.Single(result.Valide);
            Assert.Single(result.Error);
        }

        // --- UpdateLed ---

        [Fact]
        public async Task UpdateLed_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.User);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.UpdateLed(1, new UpdateLedDto());
            });
        }

        [Fact]
        public async Task UpdateLed_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateLed(999, new UpdateLedDto());
            });
        }

        [Fact]
        public async Task UpdateLed_ShouldUpdateFields_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Leds.Add(BuildLed(1, 1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            var dto = new UpdateLedDto { mqtt_led_id = 9 };
            // Act
            var result = await service.UpdateLed(1, dto);
            // Assert
            Assert.Equal(1, result.id_led);
            _validateStoreService.Verify(v => v.UpdateLedInformations(It.IsAny<Leds>(), dto), Times.Once);
        }

        // --- UpdateBulkLed ---

        [Fact]
        public async Task UpdateBulkLed_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.User);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.UpdateBulkLed(new List<UpdateBulkLedByStoreDto>(), 1);
            });
        }

        [Fact]
        public async Task UpdateBulkLed_ShouldReturnMixedResults_WhenOneMissing()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Leds.Add(BuildLed(1, 1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            var dtos = new List<UpdateBulkLedByStoreDto>
            {
                new() { id_led = 1, mqtt_led_id = 5 },
                new() { id_led = 999, mqtt_led_id = 6 }
            };
            // Act
            var result = await service.UpdateBulkLed(dtos, 1);
            // Assert
            Assert.Single(result.Valide);
            Assert.Single(result.Error);
        }

        // --- DeleteLed ---

        [Fact]
        public async Task DeleteLed_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.User);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.DeleteLed(1);
            });
        }

        [Fact]
        public async Task DeleteLed_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeleteLed(999);
            });
        }

        [Fact]
        public async Task DeleteLed_ShouldDeleteLed_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Leds.Add(BuildLed(1, 1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            // Act
            await service.DeleteLed(1);
            // Assert
            Assert.Equal(0, await context.Leds.CountAsync());
        }

        // --- DeleteBulkLed ---

        [Fact]
        public async Task DeleteBulkLed_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.User);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.DeleteBulkLed(new List<int> { 1 }, 1);
            });
        }

        [Fact]
        public async Task DeleteBulkLed_ShouldReturnMixedResults_WhenOneMissing()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Leds.Add(BuildLed(1, 1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            // Act
            var result = await service.DeleteBulkLed(new List<int> { 1, 999 }, 1);
            // Assert
            Assert.Single(result.Valide);
            Assert.Single(result.Error);
        }

        // --- ShowLedById ---

        [Fact]
        public async Task ShowLedById_ShouldThrowKeyNotFoundException_WhenLedNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.ShowLedById(1, 999, 255, 0, 0, 1000, 1);
            });
        }

        [Fact]
        public async Task ShowLedById_ShouldThrowNotImplementedException_WhenMqttNotConnected()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Leds.Add(BuildLed(1, 1));
            await context.SaveChangesAsync();
            _mqttClient.Setup(m => m.IsConnected).Returns(false);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<NotImplementedException>(async () =>
            {
                await service.ShowLedById(1, 1, 255, 0, 0, 1000, 1);
            });
        }

        [Fact]
        public async Task ShowLedById_ShouldPublishMessage_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Leds.Add(BuildLed(1, 1));
            await context.SaveChangesAsync();
            _mqttClient.Setup(m => m.IsConnected).Returns(true);
            var service = CreateService(context);
            // Act
            await service.ShowLedById(1, 1, 255, 0, 0, 1000, 1);
            // Assert
            _mqttClient.Verify(m => m.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        // --- ShowLedsByBox ---

        [Fact]
        public async Task ShowLedsByBox_ShouldThrowKeyNotFoundException_WhenStoreNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.ShowLedsByBox(1, 1, 255, 0, 0, 1000, 1);
            });
        }

        [Fact]
        public async Task ShowLedsByBox_ShouldThrowKeyNotFoundException_WhenBoxNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.ShowLedsByBox(1, 999, 255, 0, 0, 1000, 1);
            });
        }

        [Fact]
        public async Task ShowLedsByBox_ShouldThrowKeyNotFoundException_WhenNoLedsInBoxArea()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1, x: 0, y: 0, xEnd: 10, yEnd: 10));
            context.Leds.Add(BuildLed(1, 1, x: 50, y: 50));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.ShowLedsByBox(1, 1, 255, 0, 0, 1000, 1);
            });
        }

        [Fact]
        public async Task ShowLedsByBox_ShouldThrowNotImplementedException_WhenMqttNotConnected()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1, x: 0, y: 0, xEnd: 10, yEnd: 10));
            context.Leds.Add(BuildLed(1, 1, x: 5, y: 5));
            await context.SaveChangesAsync();
            _mqttClient.Setup(m => m.IsConnected).Returns(false);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<NotImplementedException>(async () =>
            {
                await service.ShowLedsByBox(1, 1, 255, 0, 0, 1000, 1);
            });
        }

        [Fact]
        public async Task ShowLedsByBox_ShouldPublishMessage_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1, x: 0, y: 0, xEnd: 10, yEnd: 10));
            context.Leds.Add(BuildLed(1, 1, x: 5, y: 5));
            await context.SaveChangesAsync();
            _mqttClient.Setup(m => m.IsConnected).Returns(true);
            var service = CreateService(context);
            // Act
            await service.ShowLedsByBox(1, 1, 255, 0, 0, 1000, 1);
            // Assert
            _mqttClient.Verify(m => m.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }
    }
}
