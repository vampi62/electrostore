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
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Services.StoreTagService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class StoreTagServiceTests : TestBase
    {
        private readonly Mock<ISessionService> _sessionService;

        public StoreTagServiceTests()
        {
            _sessionService = new Mock<ISessionService>();
        }

        private StoreTagService CreateService(ApplicationDbContext context)
        {
            return new StoreTagService(_mapper, context, _sessionService.Object);
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

        private static Tags BuildTag(int id, string name = "Tag")
        {
            return new Tags
            {
                id_tag = id,
                nom_tag = name
            };
        }

        private static StoresTags BuildStoreTag(int storeId, int tagId)
        {
            return new StoresTags
            {
                id_store = storeId,
                id_tag = tagId
            };
        }

        // --- GetStoresTagsByStoreId ---

        [Fact]
        public async Task GetStoresTagsByStoreId_ShouldThrowKeyNotFoundException_WhenStoreNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetStoresTagsByStoreId(1);
            });
        }

        [Fact]
        public async Task GetStoresTagsByStoreId_ShouldReturnOnlyTagsForGivenStore()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Stores.Add(BuildStore(2));
            context.Tags.Add(BuildTag(1));
            context.Tags.Add(BuildTag(2));
            context.StoresTags.Add(BuildStoreTag(1, 1));
            context.StoresTags.Add(BuildStoreTag(1, 2));
            context.StoresTags.Add(BuildStoreTag(2, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetStoresTagsByStoreId(1);
            // Assert
            Assert.Equal(2, result.data.Count());
            Assert.Equal(2, result.pagination.total);
        }

        // --- GetStoresTagsByTagId ---

        [Fact]
        public async Task GetStoresTagsByTagId_ShouldThrowKeyNotFoundException_WhenTagNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetStoresTagsByTagId(1);
            });
        }

        [Fact]
        public async Task GetStoresTagsByTagId_ShouldReturnOnlyStoresForGivenTag()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Stores.Add(BuildStore(2));
            context.Tags.Add(BuildTag(1));
            context.StoresTags.Add(BuildStoreTag(1, 1));
            context.StoresTags.Add(BuildStoreTag(2, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetStoresTagsByTagId(1);
            // Assert
            Assert.Equal(2, result.data.Count());
        }

        // --- GetStoreTagById ---

        [Fact]
        public async Task GetStoreTagById_ShouldReturnStoreTag_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Tags.Add(BuildTag(1));
            context.StoresTags.Add(BuildStoreTag(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetStoreTagById(1, 1);
            // Assert
            Assert.Equal(1, result.id_store);
            Assert.Equal(1, result.id_tag);
        }

        [Fact]
        public async Task GetStoreTagById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetStoreTagById(999, 999);
            });
        }

        // --- CreateStoreTag ---

        [Fact]
        public async Task CreateStoreTag_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.User);
            var service = CreateService(context);
            var dto = new CreateStoreTagDto { id_store = 1, id_tag = 1 };
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.CreateStoreTag(dto);
            });
        }

        [Fact]
        public async Task CreateStoreTag_ShouldThrowKeyNotFoundException_WhenStoreNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Tags.Add(BuildTag(1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            var dto = new CreateStoreTagDto { id_store = 999, id_tag = 1 };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreateStoreTag(dto);
            });
        }

        [Fact]
        public async Task CreateStoreTag_ShouldThrowKeyNotFoundException_WhenTagNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            var dto = new CreateStoreTagDto { id_store = 1, id_tag = 999 };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreateStoreTag(dto);
            });
        }

        [Fact]
        public async Task CreateStoreTag_ShouldThrowInvalidOperationException_WhenAlreadyExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Tags.Add(BuildTag(1));
            context.StoresTags.Add(BuildStoreTag(1, 1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            var dto = new CreateStoreTagDto { id_store = 1, id_tag = 1 };
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.CreateStoreTag(dto);
            });
        }

        [Fact]
        public async Task CreateStoreTag_ShouldCreateStoreTag_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Tags.Add(BuildTag(1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            var dto = new CreateStoreTagDto { id_store = 1, id_tag = 1 };
            // Act
            var result = await service.CreateStoreTag(dto);
            // Assert
            Assert.Equal(1, result.id_store);
            Assert.Equal(1, await context.StoresTags.CountAsync());
        }

        // --- CreateBulkStoreTag ---

        [Fact]
        public async Task CreateBulkStoreTag_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.User);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.CreateBulkStoreTag(new List<CreateStoreTagDto>());
            });
        }

        [Fact]
        public async Task CreateBulkStoreTag_ShouldReturnMixedResults_WhenOneStoreMissing()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Tags.Add(BuildTag(1));
            context.Tags.Add(BuildTag(2));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            var dtos = new List<CreateStoreTagDto>
            {
                new() { id_store = 1, id_tag = 1 },
                new() { id_store = 999, id_tag = 2 }
            };
            // Act
            var result = await service.CreateBulkStoreTag(dtos);
            // Assert
            Assert.Single(result.Valide);
            Assert.Single(result.Error);
        }

        // --- DeleteStoreTag ---

        [Fact]
        public async Task DeleteStoreTag_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.User);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.DeleteStoreTag(1, 1);
            });
        }

        [Fact]
        public async Task DeleteStoreTag_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeleteStoreTag(999, 999);
            });
        }

        [Fact]
        public async Task DeleteStoreTag_ShouldDeleteStoreTag_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Tags.Add(BuildTag(1));
            context.StoresTags.Add(BuildStoreTag(1, 1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            // Act
            await service.DeleteStoreTag(1, 1);
            // Assert
            Assert.Equal(0, await context.StoresTags.CountAsync());
        }

        // --- DeleteBulkStoreTag ---

        [Fact]
        public async Task DeleteBulkStoreTag_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.User);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.DeleteBulkStoreTag(new List<CreateStoreTagDto>());
            });
        }

        [Fact]
        public async Task DeleteBulkStoreTag_ShouldReturnMixedResults_WhenOneMissing()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.Add(BuildStore(1));
            context.Tags.Add(BuildTag(1));
            context.StoresTags.Add(BuildStoreTag(1, 1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            var dtos = new List<CreateStoreTagDto>
            {
                new() { id_store = 1, id_tag = 1 },
                new() { id_store = 999, id_tag = 999 }
            };
            // Act
            var result = await service.DeleteBulkStoreTag(dtos);
            // Assert
            Assert.Single(result.Valide);
            Assert.Single(result.Error);
        }
    }
}
