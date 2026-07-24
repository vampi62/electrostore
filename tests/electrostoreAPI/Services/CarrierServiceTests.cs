using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.CarrierService;
using ElectrostoreAPI.Services.JwiService;
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class CarrierServiceTests : TestBase
    {
        private readonly Mock<ISessionService> _sessionService;
        private readonly Mock<IJwiService> _jwiService;
        private readonly Mock<IHttpClientFactory> _httpClientFactory;

        public CarrierServiceTests()
        {
            _sessionService = new Mock<ISessionService>();
            _jwiService = new Mock<IJwiService>();
            _httpClientFactory = new Mock<IHttpClientFactory>();
        }

        private CarrierService CreateService(ApplicationDbContext context)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
            return new CarrierService(_mapper, context, _sessionService.Object, _jwiService.Object, configuration, _httpClientFactory.Object);
        }

        private void SetClientRole(UserRole role)
        {
            _sessionService.Setup(s => s.GetClientRole()).Returns(role);
        }

        private static Carriers BuildCarrier(int id, string name = "Carrier")
        {
            return new Carriers
            {
                id_carrier = id,
                key = id,
                name = name
            };
        }

        // --- GetCarriers ---

        [Fact]
        public async Task GetCarriers_ShouldReturnAllCarriers_Paginated()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Carriers.Add(BuildCarrier(1));
            context.Carriers.Add(BuildCarrier(2));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetCarriers();
            // Assert
            Assert.Equal(2, result.data.Count());
            Assert.Equal(2, result.pagination.total);
        }

        [Fact]
        public async Task GetCarriers_ShouldFilterByIdResearch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Carriers.Add(BuildCarrier(1));
            context.Carriers.Add(BuildCarrier(2));
            context.Carriers.Add(BuildCarrier(3));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetCarriers(idResearch: new List<int> { 1, 3 });
            // Assert
            Assert.Equal(2, result.data.Count());
            Assert.Equal(new[] { 1, 3 }, result.data.Select(c => c.id_carrier).OrderBy(id => id));
        }

        // --- GetCarrierById ---

        [Fact]
        public async Task GetCarrierById_ShouldReturnCarrier_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Carriers.Add(BuildCarrier(1, "DHL"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetCarrierById(1);
            // Assert
            Assert.Equal("DHL", result.name);
        }

        [Fact]
        public async Task GetCarrierById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetCarrierById(999);
            });
        }

        // --- CreateCarrier ---

        [Fact]
        public async Task CreateCarrier_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.User);
            var service = CreateService(context);
            var dto = new CreateCarrierDto { key = 1, name = "DHL" };
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.CreateCarrier(dto);
            });
        }

        [Fact]
        public async Task CreateCarrier_ShouldCreateCarrier_WhenAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            var dto = new CreateCarrierDto { key = 1, name = "DHL" };
            // Act
            var result = await service.CreateCarrier(dto);
            // Assert
            Assert.Equal("DHL", result.name);
            Assert.Equal(1, await context.Carriers.CountAsync());
        }

        // --- CreateFirstCarrier ---

        [Fact]
        public async Task CreateFirstCarrier_ShouldCreateCarrier_WithoutRoleCheck()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateCarrierDto { key = 1, name = "UPS" };
            // Act
            var result = await service.CreateFirstCarrier(dto);
            // Assert
            Assert.Equal("UPS", result.name);
            Assert.Equal(1, await context.Carriers.CountAsync());
            _sessionService.Verify(s => s.GetClientRole(), Times.Never);
        }

        // --- UpdateCarrier ---

        [Fact]
        public async Task UpdateCarrier_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.User);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.UpdateCarrier(1, new UpdateCarrierDto());
            });
        }

        [Fact]
        public async Task UpdateCarrier_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateCarrier(999, new UpdateCarrierDto());
            });
        }

        [Fact]
        public async Task UpdateCarrier_ShouldUpdateFields_WhenAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Carriers.Add(BuildCarrier(1, "Old name"));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            var dto = new UpdateCarrierDto { name = "New name", email = "contact@carrier.com" };
            // Act
            var result = await service.UpdateCarrier(1, dto);
            // Assert
            Assert.Equal("New name", result.name);
            Assert.Equal("contact@carrier.com", result.email);
        }

        // --- DeleteCarrier ---

        [Fact]
        public async Task DeleteCarrier_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.User);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.DeleteCarrier(1);
            });
        }

        [Fact]
        public async Task DeleteCarrier_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeleteCarrier(999);
            });
        }

        [Fact]
        public async Task DeleteCarrier_ShouldDeleteCarrier_WhenAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Carriers.Add(BuildCarrier(1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            // Act
            await service.DeleteCarrier(1);
            // Assert
            Assert.Equal(0, await context.Carriers.CountAsync());
        }
    }
}
