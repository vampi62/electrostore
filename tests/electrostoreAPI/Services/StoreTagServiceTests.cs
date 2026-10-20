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
        private readonly Mock<ISessionService> _sessionService = new();

        public StoreTagServiceTests()
        {
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Admin);
        }

        private StoreTagService CreateService(ApplicationDbContext context) =>
            new(_mapper, context, _sessionService.Object);

        private static Stores BuildStore(string name = "store") => new()
        {
            name_store = name,
            mqtt_name_store = name + "-mqtt"
        };

        private static Tags BuildTag(string name = "tag") => new() { name_tag = name };

        // --- GetStoresTagsByStoreId ---

        [Fact]
        public async Task GetStoresTagsByStoreId_ShouldReturnTagsLinkedToStore()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            var tag = BuildTag();
            context.Stores.Add(store);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            context.StoresTags.Add(new StoresTags { id_store = store.id_store, id_tag = tag.id_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetStoresTagsByStoreId(store.id_store);

            var single = Assert.Single(result.data);
            Assert.Equal(tag.id_tag, single.id_tag);
        }

        [Fact]
        public async Task GetStoresTagsByStoreId_ShouldThrowKeyNotFoundException_WhenStoreDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetStoresTagsByStoreId(999));
        }

        // --- GetStoresTagsByTagId ---

        [Fact]
        public async Task GetStoresTagsByTagId_ShouldReturnStoresLinkedToTag()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            var tag = BuildTag();
            context.Stores.Add(store);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            context.StoresTags.Add(new StoresTags { id_store = store.id_store, id_tag = tag.id_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetStoresTagsByTagId(tag.id_tag);

            var single = Assert.Single(result.data);
            Assert.Equal(store.id_store, single.id_store);
        }

        [Fact]
        public async Task GetStoresTagsByTagId_ShouldThrowKeyNotFoundException_WhenTagDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetStoresTagsByTagId(999));
        }

        // --- GetStoreTagById ---

        [Fact]
        public async Task GetStoreTagById_ShouldReturnStoreTag_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            var tag = BuildTag();
            context.Stores.Add(store);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            context.StoresTags.Add(new StoresTags { id_store = store.id_store, id_tag = tag.id_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetStoreTagById(store.id_store, tag.id_tag);

            Assert.Equal(store.id_store, result.id_store);
            Assert.Equal(tag.id_tag, result.id_tag);
        }

        [Fact]
        public async Task GetStoreTagById_ShouldThrowKeyNotFoundException_WhenLinkDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetStoreTagById(1, 1));
        }

        // --- CreateStoreTag ---

        [Fact]
        public async Task CreateStoreTag_ShouldPersistLink_WhenClientIsAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            var tag = BuildTag();
            context.Stores.Add(store);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateStoreTagDto { id_store = store.id_store, id_tag = tag.id_tag };

            var result = await service.CreateStoreTag(dto);

            Assert.Equal(store.id_store, result.id_store);
            Assert.Equal(1, await context.StoresTags.CountAsync());
        }

        [Fact]
        public async Task CreateStoreTag_ShouldThrowKeyNotFoundException_WhenStoreDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tag = BuildTag();
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateStoreTagDto { id_store = 999, id_tag = tag.id_tag };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateStoreTag(dto));
        }

        [Fact]
        public async Task CreateStoreTag_ShouldThrowKeyNotFoundException_WhenTagDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateStoreTagDto { id_store = store.id_store, id_tag = 999 };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateStoreTag(dto));
        }

        [Fact]
        public async Task CreateStoreTag_ShouldThrowInvalidOperationException_WhenLinkAlreadyExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            var tag = BuildTag();
            context.Stores.Add(store);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            context.StoresTags.Add(new StoresTags { id_store = store.id_store, id_tag = tag.id_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateStoreTagDto { id_store = store.id_store, id_tag = tag.id_tag };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateStoreTag(dto));
        }

        [Fact]
        public async Task CreateStoreTag_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);
            var dto = new CreateStoreTagDto { id_store = 1, id_tag = 1 };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateStoreTag(dto));
        }

        // --- DeleteStoreTag ---

        [Fact]
        public async Task DeleteStoreTag_ShouldRemoveLink_WhenClientIsAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            var tag = BuildTag();
            context.Stores.Add(store);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            context.StoresTags.Add(new StoresTags { id_store = store.id_store, id_tag = tag.id_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.DeleteStoreTag(store.id_store, tag.id_tag);

            Assert.Equal(0, await context.StoresTags.CountAsync());
        }

        [Fact]
        public async Task DeleteStoreTag_ShouldThrowKeyNotFoundException_WhenLinkDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteStoreTag(1, 1));
        }

        [Fact]
        public async Task DeleteStoreTag_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteStoreTag(1, 1));
        }

        // --- expand / bulk operations ---

        private async Task<(int parentId, int tag1Id, int tag2Id)> SeedForBulk(ApplicationDbContext context)
        {
            var parent = BuildStore();
            context.Stores.Add(parent);
            await context.SaveChangesAsync();
            var tag1 = BuildTag("bulk-1");
            var tag2 = BuildTag("bulk-2");
            context.Tags.AddRange(tag1, tag2);
            await context.SaveChangesAsync();
            return (parent.id_store, tag1.id_tag, tag2.id_tag);
        }

        [Fact]
        public async Task GetStoresTagsByStoreId_ShouldExpandParentAndTag_WhenRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parentId, tagId, _) = await SeedForBulk(context);
            context.StoresTags.Add(new StoresTags { id_store = parentId, id_tag = tagId });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetStoresTagsByStoreId(parentId, expand: ["store", "tag"]);

            var link = Assert.Single(result.data);
            Assert.NotNull(link.store);
            Assert.NotNull(link.tag);
        }

        [Fact]
        public async Task GetStoresTagsByTagId_ShouldExpandParentAndTag_WhenRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parentId, tagId, _) = await SeedForBulk(context);
            context.StoresTags.Add(new StoresTags { id_store = parentId, id_tag = tagId });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetStoresTagsByTagId(tagId, expand: ["store", "tag"]);

            var link = Assert.Single(result.data);
            Assert.NotNull(link.store);
            Assert.NotNull(link.tag);
        }

        [Fact]
        public async Task GetStoreTagById_ShouldExpandParentAndTag_WhenRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parentId, tagId, _) = await SeedForBulk(context);
            context.StoresTags.Add(new StoresTags { id_store = parentId, id_tag = tagId });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetStoreTagById(parentId, tagId, ["store", "tag"]);

            Assert.NotNull(result.store);
            Assert.NotNull(result.tag);
        }

        [Fact]
        public async Task CreateBulkStoreTag_ShouldReturnValidAndErrorEntries()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parentId, tag1Id, _) = await SeedForBulk(context);
            var service = CreateService(context);

            var result = await service.CreateBulkStoreTag(
            [
                new CreateStoreTagDto { id_store = parentId, id_tag = tag1Id },
                new CreateStoreTagDto { id_store = 9999, id_tag = tag1Id },
                new CreateStoreTagDto { id_store = parentId, id_tag = tag1Id }
            ]);

            Assert.Single(result.valide);
            Assert.Equal(2, result.error.Count());
            Assert.Equal(1, await context.StoresTags.CountAsync());
        }

        [Fact]
        public async Task CreateBulkStoreTag_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateBulkStoreTag([]));
        }

        [Fact]
        public async Task DeleteBulkStoreTag_ShouldReturnValidAndErrorEntries()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parentId, tag1Id, tag2Id) = await SeedForBulk(context);
            context.StoresTags.Add(new StoresTags { id_store = parentId, id_tag = tag1Id });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.DeleteBulkStoreTag(
            [
                new CreateStoreTagDto { id_store = parentId, id_tag = tag1Id },
                new CreateStoreTagDto { id_store = parentId, id_tag = tag2Id }
            ]);

            var deleted = Assert.Single(result.valide);
            Assert.Equal(tag1Id, deleted.id_tag);
            Assert.Single(result.error);
            Assert.Equal(0, await context.StoresTags.CountAsync());
        }

        [Fact]
        public async Task DeleteBulkStoreTag_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteBulkStoreTag([]));
        }
    }
}
