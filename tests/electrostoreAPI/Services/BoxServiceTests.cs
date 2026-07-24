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
        private readonly Mock<ISessionService> _sessionService;
        private readonly Mock<IValidateStoreService> _validateStoreService;

        public BoxServiceTests()
        {
            _sessionService = new Mock<ISessionService>();
            _validateStoreService = new Mock<IValidateStoreService>();
        }

        private BoxService CreateService(ApplicationDbContext context)
        {
            return new BoxService(_mapper, context, _sessionService.Object, _validateStoreService.Object);
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

        private static Boxs BuildBox(int id, int storeId, int x = 0, int y = 0)
        {
            return new Boxs
            {
                id_box = id,
                id_store = storeId,
                xstart_box = x,
                ystart_box = y,
                xend_box = x + 10,
                yend_box = y + 10
            };
        }

        private static CreateBoxDto BuildCreateBoxDto(int storeId, int x = 0, int y = 0)
        {
            return new CreateBoxDto
            {
                xstart_box = x,
                ystart_box = y,
                xend_box = x + 10,
                yend_box = y + 10,
                id_store = storeId
            };
        }

        // --- GetBoxsByStoreId ---

        [Fact]
        public async Task GetBoxsByStoreId_ShouldThrowKeyNotFoundException_WhenStoreNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var boxService = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await boxService.GetBoxsByStoreId(1);
            });
        }

        [Fact]
        public async Task GetBoxsByStoreId_ShouldReturnOnlyBoxesForGivenStore()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Stores.Add(BuildStore(2));
            context.Boxs.Add(BuildBox(1, 1, 0, 0));
            context.Boxs.Add(BuildBox(2, 1, 20, 20));
            context.Boxs.Add(BuildBox(3, 2, 0, 0));
            await context.SaveChangesAsync();
            var boxService = CreateService(context);
            // Act
            var result = await boxService.GetBoxsByStoreId(1);
            // Assert
            Assert.Equal(2, result.data.Count());
            Assert.All(result.data, b => Assert.Equal(1, b.id_store));
            Assert.Equal(2, result.pagination.total);
        }

        [Fact]
        public async Task GetBoxsByStoreId_ShouldRespectLimitAndOffset()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            for (var i = 1; i <= 5; i++)
            {
                context.Boxs.Add(BuildBox(i, 1, i * 10, 0));
            }
            await context.SaveChangesAsync();
            var boxService = CreateService(context);
            // Act
            var result = await boxService.GetBoxsByStoreId(1, limit: 2, offset: 1);
            // Assert
            Assert.Equal(2, result.data.Count());
            Assert.Equal(new[] { 2, 3 }, result.data.Select(b => b.id_box));
            Assert.Equal(5, result.pagination.total);
            Assert.True(result.pagination.hasMore);
        }

        [Fact]
        public async Task GetBoxsByStoreId_ShouldExpandStore_WhenRequested()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1, "MyStore"));
            context.Boxs.Add(BuildBox(1, 1));
            await context.SaveChangesAsync();
            var boxService = CreateService(context);
            // Act
            var result = await boxService.GetBoxsByStoreId(1, expand: new List<string> { "store" });
            // Assert
            var box = Assert.Single(result.data);
            Assert.NotNull(box.store);
            Assert.Equal("MyStore", box.store!.nom_store);
        }

        // --- GetBoxById ---

        [Fact]
        public async Task GetBoxById_ShouldReturnBox_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            await context.SaveChangesAsync();
            var boxService = CreateService(context);
            // Act
            var result = await boxService.GetBoxById(1);
            // Assert
            Assert.Equal(1, result.id_box);
            Assert.Equal(1, result.id_store);
        }

        [Fact]
        public async Task GetBoxById_ShouldThrowKeyNotFoundException_WhenBoxDoesNotExist()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var boxService = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await boxService.GetBoxById(999);
            });
        }

        [Fact]
        public async Task GetBoxById_ShouldThrowKeyNotFoundException_WhenStoreIdDoesNotMatch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Stores.Add(BuildStore(2));
            context.Boxs.Add(BuildBox(1, 1));
            await context.SaveChangesAsync();
            var boxService = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await boxService.GetBoxById(1, storeId: 2);
            });
        }

        // --- CreateBox ---

        [Fact]
        public async Task CreateBox_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.User);
            var boxService = CreateService(context);
            var dto = BuildCreateBoxDto(1);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await boxService.CreateBox(dto);
            });
        }

        [Fact]
        public async Task CreateBox_ShouldThrowKeyNotFoundException_WhenStoreNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.Admin);
            var boxService = CreateService(context);
            var dto = BuildCreateBoxDto(1);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await boxService.CreateBox(dto);
            });
        }

        [Fact]
        public async Task CreateBox_ShouldCreateBox_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var boxService = CreateService(context);
            var dto = BuildCreateBoxDto(1);
            // Act
            var result = await boxService.CreateBox(dto);
            // Assert
            Assert.Equal(1, result.id_store);
            Assert.Equal(1, await context.Boxs.CountAsync());
            _validateStoreService.Verify(v => v.CheckCreateBoxPositionOverlap(dto), Times.Once);
            _validateStoreService.Verify(v => v.ValidateBoxPosition(It.IsAny<Boxs>(), It.IsAny<Stores>()), Times.Once);
        }

        // --- CreateBulkBox ---

        [Fact]
        public async Task CreateBulkBox_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.User);
            var boxService = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await boxService.CreateBulkBox(new List<CreateBoxDto>());
            });
        }

        [Fact]
        public async Task CreateBulkBox_ShouldNotSaveAnything_WhenOneItemFails()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var boxService = CreateService(context);
            var boxes = new List<CreateBoxDto>
            {
                BuildCreateBoxDto(1),
                BuildCreateBoxDto(999, x: 20, y: 20)
            };
            // Act
            var result = await boxService.CreateBulkBox(boxes);
            // Assert
            Assert.Single(result.Valide);
            Assert.Single(result.Error);
            Assert.Equal(0, await context.Boxs.CountAsync());
        }

        [Fact]
        public async Task CreateBulkBox_ShouldSaveAll_WhenAllValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var boxService = CreateService(context);
            var boxes = new List<CreateBoxDto>
            {
                BuildCreateBoxDto(1),
                BuildCreateBoxDto(1, x: 20, y: 20)
            };
            // Act
            var result = await boxService.CreateBulkBox(boxes);
            // Assert
            Assert.Equal(2, result.Valide.Count);
            Assert.Empty(result.Error);
            Assert.Equal(2, await context.Boxs.CountAsync());
        }

        // --- UpdateBox ---

        [Fact]
        public async Task UpdateBox_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.User);
            var boxService = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await boxService.UpdateBox(1, new UpdateBoxDto());
            });
        }

        [Fact]
        public async Task UpdateBox_ShouldThrowKeyNotFoundException_WhenBoxNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.Admin);
            var boxService = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await boxService.UpdateBox(999, new UpdateBoxDto());
            });
        }

        [Fact]
        public async Task UpdateBox_ShouldThrowKeyNotFoundException_WhenStoreIdDoesNotMatch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Stores.Add(BuildStore(2));
            context.Boxs.Add(BuildBox(1, 1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var boxService = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await boxService.UpdateBox(1, new UpdateBoxDto(), storeId: 2);
            });
        }

        [Fact]
        public async Task UpdateBox_ShouldSaveChanges_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var boxService = CreateService(context);
            var dto = new UpdateBoxDto { xstart_box = 5 };
            // Act
            var result = await boxService.UpdateBox(1, dto);
            // Assert
            Assert.Equal(1, result.id_box);
            _validateStoreService.Verify(v => v.UpdateBoxInformations(It.IsAny<Boxs>(), dto), Times.Once);
            _validateStoreService.Verify(v => v.ValidateBoxPosition(It.IsAny<Boxs>(), It.IsAny<Stores>()), Times.Once);
            _validateStoreService.Verify(v => v.CheckUpdateBoxPositionOverlap(It.IsAny<Boxs>()), Times.Once);
        }

        // --- UpdateBulkBox ---

        [Fact]
        public async Task UpdateBulkBox_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.User);
            var boxService = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await boxService.UpdateBulkBox(new List<UpdateBulkBoxByStoreDto>());
            });
        }

        [Fact]
        public async Task UpdateBulkBox_ShouldReturnError_WhenBoxNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.Admin);
            var boxService = CreateService(context);
            var boxes = new List<UpdateBulkBoxByStoreDto> { new() { id_box = 999 } };
            // Act
            var result = await boxService.UpdateBulkBox(boxes);
            // Assert
            Assert.Empty(result.Valide);
            Assert.Single(result.Error);
        }

        [Fact]
        public async Task UpdateBulkBox_ShouldSaveAll_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            context.Boxs.Add(BuildBox(2, 1, 20, 20));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var boxService = CreateService(context);
            var boxes = new List<UpdateBulkBoxByStoreDto>
            {
                new() { id_box = 1, xstart_box = 1 },
                new() { id_box = 2, xstart_box = 21 }
            };
            // Act
            var result = await boxService.UpdateBulkBox(boxes);
            // Assert
            Assert.Equal(2, result.Valide.Count);
            Assert.Empty(result.Error);
        }

        [Fact]
        public async Task UpdateBulkBox_ShouldNotSave_WhenPositionOverlapCheckFails()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            _validateStoreService.Setup(v => v.CheckUpdateBoxPositionOverlap(It.IsAny<Boxs>())).ThrowsAsync(new InvalidOperationException("overlap"));
            var boxService = CreateService(context);
            var boxes = new List<UpdateBulkBoxByStoreDto> { new() { id_box = 1, xstart_box = 1 } };
            // Act
            var result = await boxService.UpdateBulkBox(boxes);
            // Assert
            // the box is still listed as "valid" from the first pass, but the second pass
            // (overlap check) adds it to errors too, so SaveChangesAsync is skipped and
            // the change is not persisted.
            Assert.Single(result.Valide);
            Assert.Single(result.Error);
            var box = await context.Boxs.FindAsync(1);
            Assert.Equal(0, box!.xstart_box);
        }

        // --- DeleteBox ---

        [Fact]
        public async Task DeleteBox_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.User);
            var boxService = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await boxService.DeleteBox(1);
            });
        }

        [Fact]
        public async Task DeleteBox_ShouldThrowKeyNotFoundException_WhenBoxNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.Admin);
            var boxService = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await boxService.DeleteBox(999);
            });
        }

        [Fact]
        public async Task DeleteBox_ShouldThrowInvalidOperationException_WhenBoxHasItems()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            context.ItemsBoxs.Add(new ItemsBoxs { id_box = 1, id_item = 1, qte_item_box = 5 });
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var boxService = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await boxService.DeleteBox(1);
            });
        }

        [Fact]
        public async Task DeleteBox_ShouldDeleteBox_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var boxService = CreateService(context);
            // Act
            await boxService.DeleteBox(1);
            // Assert
            Assert.Equal(0, await context.Boxs.CountAsync());
        }

        // --- DeleteBulkBox ---

        [Fact]
        public async Task DeleteBulkBox_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.User);
            var boxService = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await boxService.DeleteBulkBox(new List<int> { 1 }, 1);
            });
        }

        [Fact]
        public async Task DeleteBulkBox_ShouldReturnMixedResults_WhenSomeIdsInvalid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var boxService = CreateService(context);
            // Act
            var result = await boxService.DeleteBulkBox(new List<int> { 1, 999 }, 1);
            // Assert
            Assert.Single(result.Valide);
            Assert.Single(result.Error);
            Assert.Equal(0, await context.Boxs.CountAsync());
        }
    }
}
