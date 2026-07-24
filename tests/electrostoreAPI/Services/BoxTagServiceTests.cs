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
using ElectrostoreAPI.Services.BoxTagService;
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class BoxTagServiceTests : TestBase
    {
        private readonly Mock<ISessionService> _sessionService;

        public BoxTagServiceTests()
        {
            _sessionService = new Mock<ISessionService>();
        }

        private BoxTagService CreateService(ApplicationDbContext context)
        {
            return new BoxTagService(_mapper, context, _sessionService.Object);
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
                mqtt_name_store = "mqtt-" + id
            };
        }

        private static Boxs BuildBox(int id, int storeId)
        {
            return new Boxs
            {
                id_box = id,
                id_store = storeId,
                xend_box = 10,
                yend_box = 10
            };
        }

        private static Tags BuildTag(int id, string name = "Tag")
        {
            return new Tags
            {
                id_tag = id,
                nom_tag = name
            };
        }

        private static BoxsTags BuildBoxTag(int boxId, int tagId)
        {
            return new BoxsTags
            {
                id_box = boxId,
                id_tag = tagId
            };
        }

        // --- GetBoxsTagsByBoxId ---

        [Fact]
        public async Task GetBoxsTagsByBoxId_ShouldThrowKeyNotFoundException_WhenBoxNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetBoxsTagsByBoxId(1);
            });
        }

        [Fact]
        public async Task GetBoxsTagsByBoxId_ShouldReturnOnlyTagsForGivenBox()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            context.Boxs.Add(BuildBox(2, 1));
            context.Tags.Add(BuildTag(1));
            context.Tags.Add(BuildTag(2));
            context.BoxsTags.Add(BuildBoxTag(1, 1));
            context.BoxsTags.Add(BuildBoxTag(1, 2));
            context.BoxsTags.Add(BuildBoxTag(2, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetBoxsTagsByBoxId(1);
            // Assert
            Assert.Equal(2, result.data.Count());
        }

        // --- GetBoxsTagsByTagId ---

        [Fact]
        public async Task GetBoxsTagsByTagId_ShouldThrowKeyNotFoundException_WhenTagNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetBoxsTagsByTagId(1);
            });
        }

        [Fact]
        public async Task GetBoxsTagsByTagId_ShouldReturnOnlyBoxesForGivenTag()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            context.Boxs.Add(BuildBox(2, 1));
            context.Tags.Add(BuildTag(1));
            context.BoxsTags.Add(BuildBoxTag(1, 1));
            context.BoxsTags.Add(BuildBoxTag(2, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetBoxsTagsByTagId(1);
            // Assert
            Assert.Equal(2, result.data.Count());
        }

        // --- GetBoxTagById ---

        [Fact]
        public async Task GetBoxTagById_ShouldReturnBoxTag_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            context.Tags.Add(BuildTag(1));
            context.BoxsTags.Add(BuildBoxTag(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetBoxTagById(1, 1);
            // Assert
            Assert.Equal(1, result.id_box);
            Assert.Equal(1, result.id_tag);
        }

        [Fact]
        public async Task GetBoxTagById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetBoxTagById(999, 999);
            });
        }

        // --- CreateBoxTag ---

        [Fact]
        public async Task CreateBoxTag_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.User);
            var service = CreateService(context);
            var dto = new CreateBoxTagDto { id_box = 1, id_tag = 1 };
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.CreateBoxTag(dto);
            });
        }

        [Fact]
        public async Task CreateBoxTag_ShouldThrowKeyNotFoundException_WhenBoxNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Tags.Add(BuildTag(1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            var dto = new CreateBoxTagDto { id_box = 999, id_tag = 1 };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreateBoxTag(dto);
            });
        }

        [Fact]
        public async Task CreateBoxTag_ShouldThrowKeyNotFoundException_WhenTagNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            var dto = new CreateBoxTagDto { id_box = 1, id_tag = 999 };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreateBoxTag(dto);
            });
        }

        [Fact]
        public async Task CreateBoxTag_ShouldThrowInvalidOperationException_WhenAlreadyExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            context.Tags.Add(BuildTag(1));
            context.BoxsTags.Add(BuildBoxTag(1, 1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            var dto = new CreateBoxTagDto { id_box = 1, id_tag = 1 };
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.CreateBoxTag(dto);
            });
        }

        [Fact]
        public async Task CreateBoxTag_ShouldCreateBoxTag_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            context.Tags.Add(BuildTag(1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            var dto = new CreateBoxTagDto { id_box = 1, id_tag = 1 };
            // Act
            var result = await service.CreateBoxTag(dto);
            // Assert
            Assert.Equal(1, result.id_box);
            Assert.Equal(1, await context.BoxsTags.CountAsync());
        }

        // --- CreateBulkBoxTag ---

        [Fact]
        public async Task CreateBulkBoxTag_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.User);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.CreateBulkBoxTag(new List<CreateBoxTagDto>());
            });
        }

        [Fact]
        public async Task CreateBulkBoxTag_ShouldReturnMixedResults_WhenOneBoxMissing()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            context.Tags.Add(BuildTag(1));
            context.Tags.Add(BuildTag(2));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            var dtos = new List<CreateBoxTagDto>
            {
                new() { id_box = 1, id_tag = 1 },
                new() { id_box = 999, id_tag = 2 }
            };
            // Act
            var result = await service.CreateBulkBoxTag(dtos);
            // Assert
            Assert.Single(result.Valide);
            Assert.Single(result.Error);
        }

        // --- DeleteBoxTag ---

        [Fact]
        public async Task DeleteBoxTag_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.User);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.DeleteBoxTag(1, 1);
            });
        }

        [Fact]
        public async Task DeleteBoxTag_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeleteBoxTag(999, 999);
            });
        }

        [Fact]
        public async Task DeleteBoxTag_ShouldDeleteBoxTag_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            context.Tags.Add(BuildTag(1));
            context.BoxsTags.Add(BuildBoxTag(1, 1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            // Act
            await service.DeleteBoxTag(1, 1);
            // Assert
            Assert.Equal(0, await context.BoxsTags.CountAsync());
        }

        // --- DeleteBulkBoxTag ---

        [Fact]
        public async Task DeleteBulkBoxTag_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.User);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.DeleteBulkBoxTag(new List<CreateBoxTagDto>());
            });
        }

        [Fact]
        public async Task DeleteBulkBoxTag_ShouldReturnMixedResults_WhenOneMissing()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            context.Tags.Add(BuildTag(1));
            context.BoxsTags.Add(BuildBoxTag(1, 1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            var dtos = new List<CreateBoxTagDto>
            {
                new() { id_box = 1, id_tag = 1 },
                new() { id_box = 999, id_tag = 999 }
            };
            // Act
            var result = await service.DeleteBulkBoxTag(dtos);
            // Assert
            Assert.Single(result.Valide);
            Assert.Single(result.Error);
        }

        // --- CheckIfStoreExists ---

        [Fact]
        public async Task CheckIfStoreExists_ShouldThrowKeyNotFoundException_WhenBoxNotInStore()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Stores.Add(BuildStore(2));
            context.Boxs.Add(BuildBox(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CheckIfStoreExists(2, 1);
            });
        }

        [Fact]
        public async Task CheckIfStoreExists_ShouldNotThrow_WhenBoxBelongsToStore()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Boxs.Add(BuildBox(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var exception = await Record.ExceptionAsync(async () =>
            {
                await service.CheckIfStoreExists(1, 1);
            });
            // Assert
            Assert.Null(exception);
        }
    }
}
