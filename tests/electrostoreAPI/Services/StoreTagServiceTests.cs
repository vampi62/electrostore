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
    }
}
