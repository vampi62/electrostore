using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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

        // --- additional coverage: expand, images and sizes ---

        private static Mock<Microsoft.AspNetCore.Http.IFormFile> BuildImageFile(string fileName = "plan.png", string contentType = "image/png")
        {
            var file = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
            file.Setup(f => f.FileName).Returns(fileName);
            file.Setup(f => f.ContentType).Returns(contentType);
            file.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream([1, 2, 3]));
            return file;
        }

        private void SetupImageSaving()
        {
            _fileService.SetupImageWithThumbnailSaving("zoneImages/1/plan.png", "zoneImagesThumbnails/1/plan.png");
        }

        private static Stores BuildStore(int zoneId) => new()
        {
            name_store = "store",
            mqtt_name_store = "store-mqtt",
            xlength_store = 5,
            ylength_store = 5,
            id_zone = zoneId
        };

        [Fact]
        public async Task GetZones_ShouldRestrictToIds_WhenAnyFilterProvided()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var a = BuildZone("a");
            var b = BuildZone("b");
            context.Zones.AddRange(a, b);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetZones(rsql: [new FilterDto { field = "id_zone", search_type = "any", value = b.id_zone.ToString() }]);

            var single = Assert.Single(result.data);
            Assert.Equal(b.id_zone, single.id_zone);
        }

        [Fact]
        public async Task GetZones_ShouldReturnStoresCountAndStores_WhenExpanded()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var zone = BuildZone();
            context.Zones.Add(zone);
            await context.SaveChangesAsync();
            context.Stores.Add(BuildStore(zone.id_zone));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var expanded = await service.GetZones(expand: ["stores"]);
            var plain = await service.GetZones();

            var expandedZone = Assert.Single(expanded.data);
            Assert.Equal(1, expandedZone.stores_count);
            Assert.Single(expandedZone.stores!);
            var plainZone = Assert.Single(plain.data);
            Assert.Equal(1, plainZone.stores_count);
        }

        [Fact]
        public async Task GetZoneById_ShouldReturnStores_WhenExpanded()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var zone = BuildZone();
            context.Zones.Add(zone);
            await context.SaveChangesAsync();
            context.Stores.Add(BuildStore(zone.id_zone));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetZoneById(zone.id_zone, ["stores"]);

            Assert.Equal(1, result.stores_count);
            Assert.Single(result.stores!);
        }

        [Fact]
        public async Task CreateZone_ShouldSaveImageAndThumbnail_WhenImageProvided()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetupImageSaving();
            var service = CreateService(context);

            var result = await service.CreateZone(new CreateZoneDto
            {
                name_zone = "with image",
                xlength_zone = 10,
                ylength_zone = 10,
                img_file = BuildImageFile().Object
            });

            Assert.Equal("zoneImages/1/plan.png", result.url_picture_zone);
            Assert.Equal("zoneImagesThumbnails/1/plan.png", result.url_thumbnail_zone);
            _fileService.Verify(f => f.GetFile("zoneImages/1/plan.png"), Times.Once);
        }

        [Fact]
        public async Task UpdateZone_ShouldUpdateDescriptionAndSize_WhenStoresStillFit()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var zone = BuildZone();
            context.Zones.Add(zone);
            await context.SaveChangesAsync();
            var store = BuildStore(zone.id_zone);
            store.xmax_store = 40;
            store.ymax_store = 40;
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateZone(zone.id_zone, new UpdateZoneDto { description_zone = "desc", xlength_zone = 50, ylength_zone = 60 });

            Assert.Equal("desc", result.description_zone);
            Assert.Equal(50, result.xlength_zone);
            Assert.Equal(60, result.ylength_zone);
        }

        [Fact]
        public async Task UpdateZone_ShouldKeepOtherDimension_WhenOnlyOneIsProvided()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var zone = BuildZone();
            context.Zones.Add(zone);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateZone(zone.id_zone, new UpdateZoneDto { xlength_zone = 30 });

            Assert.Equal(30, result.xlength_zone);
            Assert.Equal(100, result.ylength_zone);
        }

        [Fact]
        public async Task UpdateZone_ShouldReplaceImage_AndDeleteOldFiles()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var zone = BuildZone();
            zone.url_picture_zone = "zoneImages/1/old.png";
            zone.url_thumbnail_zone = "zoneImagesThumbnails/1/old.png";
            context.Zones.Add(zone);
            await context.SaveChangesAsync();
            SetupImageSaving();
            var service = CreateService(context);

            var result = await service.UpdateZone(zone.id_zone, new UpdateZoneDto { img_file = BuildImageFile().Object });

            Assert.Equal("zoneImages/1/plan.png", result.url_picture_zone);
            Assert.Equal("zoneImagesThumbnails/1/plan.png", result.url_thumbnail_zone);
            _fileService.Verify(f => f.DeleteFile("zoneImages/1/old.png"), Times.Once);
            _fileService.Verify(f => f.DeleteFile("zoneImagesThumbnails/1/old.png"), Times.Once);
        }

        [Fact]
        public async Task UpdateZone_ShouldAddImage_WhenZoneHadNone()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var zone = BuildZone();
            context.Zones.Add(zone);
            await context.SaveChangesAsync();
            SetupImageSaving();
            var service = CreateService(context);

            var result = await service.UpdateZone(zone.id_zone, new UpdateZoneDto { img_file = BuildImageFile().Object });

            Assert.NotNull(result.url_picture_zone);
            _fileService.Verify(f => f.DeleteFile(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateZone_ShouldRemoveImage_WhenUnsetRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var zone = BuildZone();
            zone.url_picture_zone = "zoneImages/1/old.png";
            zone.url_thumbnail_zone = "zoneImagesThumbnails/1/old.png";
            context.Zones.Add(zone);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateZone(zone.id_zone, new UpdateZoneDto { unset_img_zone = true, img_file = BuildImageFile().Object });

            Assert.Null(result.url_picture_zone);
            Assert.Null(result.url_thumbnail_zone);
            _fileService.Verify(f => f.DeleteFile("zoneImages/1/old.png"), Times.Once);
            _fileService.Verify(f => f.DeleteFile("zoneImagesThumbnails/1/old.png"), Times.Once);
            _fileService.Verify(f => f.SaveFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task UpdateZone_ShouldNotTouchFiles_WhenUnsetRequestedButNoImageExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var zone = BuildZone();
            context.Zones.Add(zone);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.UpdateZone(zone.id_zone, new UpdateZoneDto { unset_img_zone = true });

            _fileService.Verify(f => f.DeleteFile(It.IsAny<string>()), Times.Never);
        }
    }
}
