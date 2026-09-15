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
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Services.ZoneService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class ZoneServiceTests : TestBase
    {
        private readonly Mock<ISessionService> _sessionService = new();
        private readonly Mock<IFileService> _fileService = new();

        public ZoneServiceTests()
        {
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Admin);
        }

        private ZoneService CreateService(ApplicationDbContext context) =>
            new(_mapper, context, _sessionService.Object, _fileService.Object);

        private static Zones BuildZone(string name = "zone") => new()
        {
            name_zone = name,
            xlength_zone = 100,
            ylength_zone = 100
        };

        // --- GetZones ---

        [Fact]
        public async Task GetZones_ShouldReturnAllZones_WhenNoFilterApplied()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Zones.AddRange(BuildZone("a"), BuildZone("b"));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetZones();

            Assert.Equal(2, result.pagination.total);
            Assert.Equal(2, result.data.Count());
        }

        // --- GetZoneById ---

        [Fact]
        public async Task GetZoneById_ShouldReturnZone_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var zone = BuildZone();
            context.Zones.Add(zone);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetZoneById(zone.id_zone);

            Assert.Equal(zone.id_zone, result.id_zone);
            Assert.Equal(0, result.stores_count);
        }

        [Fact]
        public async Task GetZoneById_ShouldThrowKeyNotFoundException_WhenZoneDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetZoneById(999));
        }

        // --- CreateZone ---

        [Fact]
        public async Task CreateZone_ShouldPersistZone_AndCreateDirectories_WhenClientIsAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateZoneDto { name_zone = "new-zone", xlength_zone = 50, ylength_zone = 60 };

            var result = await service.CreateZone(dto);

            Assert.Equal("new-zone", result.name_zone);
            Assert.Equal(1, await context.Zones.CountAsync());
            _fileService.Verify(f => f.CreateDirectory(It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public async Task CreateZone_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);
            var dto = new CreateZoneDto { name_zone = "new-zone", xlength_zone = 50, ylength_zone = 60 };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateZone(dto));
            Assert.Equal(0, await context.Zones.CountAsync());
        }

        // --- UpdateZone ---

        [Fact]
        public async Task UpdateZone_ShouldUpdateProvidedFields_WhenClientIsAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var zone = BuildZone();
            context.Zones.Add(zone);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new UpdateZoneDto { name_zone = "renamed" };

            var result = await service.UpdateZone(zone.id_zone, dto);

            Assert.Equal("renamed", result.name_zone);
        }

        [Fact]
        public async Task UpdateZone_ShouldThrowArgumentException_WhenNewSizeWouldPlaceStoreOutOfBounds()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var zone = BuildZone();
            context.Zones.Add(zone);
            await context.SaveChangesAsync();
            context.Stores.Add(new Stores
            {
                name_store = "store",
                mqtt_name_store = "store-mqtt",
                id_zone = zone.id_zone,
                xlength_store = 10,
                ylength_store = 10,
                xmax_store = 200,
                ymax_store = 50
            });
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new UpdateZoneDto { xlength_zone = 50 };

            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateZone(zone.id_zone, dto));
        }

        [Fact]
        public async Task UpdateZone_ShouldThrowKeyNotFoundException_WhenZoneDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateZone(999, new UpdateZoneDto()));
        }

        [Fact]
        public async Task UpdateZone_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var zone = BuildZone();
            context.Zones.Add(zone);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateZone(zone.id_zone, new UpdateZoneDto { name_zone = "x" }));
        }

        // --- DeleteZone ---

        [Fact]
        public async Task DeleteZone_ShouldRemoveZone_AndDeleteDirectories_WhenClientIsAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var zone = BuildZone();
            context.Zones.Add(zone);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.DeleteZone(zone.id_zone);

            Assert.Equal(0, await context.Zones.CountAsync());
            _fileService.Verify(f => f.DeleteDirectory(It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public async Task DeleteZone_ShouldThrowKeyNotFoundException_WhenZoneDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteZone(999));
        }

        [Fact]
        public async Task DeleteZone_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var zone = BuildZone();
            context.Zones.Add(zone);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteZone(zone.id_zone));
            Assert.Equal(1, await context.Zones.CountAsync());
        }
    }
}
