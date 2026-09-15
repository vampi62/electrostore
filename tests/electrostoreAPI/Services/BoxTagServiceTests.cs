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
        private readonly Mock<ISessionService> _sessionService = new();

        public BoxTagServiceTests()
        {
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Admin);
        }

        private BoxTagService CreateService(ApplicationDbContext context) =>
            new(_mapper, context, _sessionService.Object);

        private static Stores BuildStore(string name = "store") => new()
        {
            name_store = name,
            mqtt_name_store = name + "-mqtt"
        };

        private static Boxs BuildBox(int storeId) => new()
        {
            id_store = storeId,
            xstart_box = 0,
            ystart_box = 0,
            xend_box = 5,
            yend_box = 5
        };

        private static Tags BuildTag(string name = "tag") => new() { name_tag = name };

        // --- GetBoxsTagsByBoxId ---

        [Fact]
        public async Task GetBoxsTagsByBoxId_ShouldReturnTagsLinkedToBox()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = BuildBox(store.id_store);
            var tag = BuildTag();
            context.Boxs.Add(box);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            context.BoxsTags.Add(new BoxsTags { id_box = box.id_box, id_tag = tag.id_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetBoxsTagsByBoxId(box.id_box);

            var single = Assert.Single(result.data);
            Assert.Equal(tag.id_tag, single.id_tag);
        }

        [Fact]
        public async Task GetBoxsTagsByBoxId_ShouldThrowKeyNotFoundException_WhenBoxDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetBoxsTagsByBoxId(999));
        }

        // --- GetBoxsTagsByTagId ---

        [Fact]
        public async Task GetBoxsTagsByTagId_ShouldReturnBoxesLinkedToTag()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = BuildBox(store.id_store);
            var tag = BuildTag();
            context.Boxs.Add(box);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            context.BoxsTags.Add(new BoxsTags { id_box = box.id_box, id_tag = tag.id_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetBoxsTagsByTagId(tag.id_tag);

            var single = Assert.Single(result.data);
            Assert.Equal(box.id_box, single.id_box);
        }

        [Fact]
        public async Task GetBoxsTagsByTagId_ShouldThrowKeyNotFoundException_WhenTagDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetBoxsTagsByTagId(999));
        }

        // --- GetBoxTagById ---

        [Fact]
        public async Task GetBoxTagById_ShouldReturnBoxTag_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = BuildBox(store.id_store);
            var tag = BuildTag();
            context.Boxs.Add(box);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            context.BoxsTags.Add(new BoxsTags { id_box = box.id_box, id_tag = tag.id_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetBoxTagById(box.id_box, tag.id_tag);

            Assert.Equal(box.id_box, result.id_box);
            Assert.Equal(tag.id_tag, result.id_tag);
        }

        [Fact]
        public async Task GetBoxTagById_ShouldThrowKeyNotFoundException_WhenLinkDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetBoxTagById(1, 1));
        }

        // --- CreateBoxTag ---

        [Fact]
        public async Task CreateBoxTag_ShouldPersistLink_WhenClientIsAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = BuildBox(store.id_store);
            var tag = BuildTag();
            context.Boxs.Add(box);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateBoxTagDto { id_box = box.id_box, id_tag = tag.id_tag };

            var result = await service.CreateBoxTag(dto);

            Assert.Equal(box.id_box, result.id_box);
            Assert.Equal(1, await context.BoxsTags.CountAsync());
        }

        [Fact]
        public async Task CreateBoxTag_ShouldThrowKeyNotFoundException_WhenBoxDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tag = BuildTag();
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateBoxTagDto { id_box = 999, id_tag = tag.id_tag };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateBoxTag(dto));
        }

        [Fact]
        public async Task CreateBoxTag_ShouldThrowKeyNotFoundException_WhenTagDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = BuildBox(store.id_store);
            context.Boxs.Add(box);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateBoxTagDto { id_box = box.id_box, id_tag = 999 };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateBoxTag(dto));
        }

        [Fact]
        public async Task CreateBoxTag_ShouldThrowInvalidOperationException_WhenLinkAlreadyExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = BuildBox(store.id_store);
            var tag = BuildTag();
            context.Boxs.Add(box);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            context.BoxsTags.Add(new BoxsTags { id_box = box.id_box, id_tag = tag.id_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateBoxTagDto { id_box = box.id_box, id_tag = tag.id_tag };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateBoxTag(dto));
        }

        [Fact]
        public async Task CreateBoxTag_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);
            var dto = new CreateBoxTagDto { id_box = 1, id_tag = 1 };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateBoxTag(dto));
        }

        // --- DeleteBoxTag ---

        [Fact]
        public async Task DeleteBoxTag_ShouldRemoveLink_WhenClientIsAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = BuildBox(store.id_store);
            var tag = BuildTag();
            context.Boxs.Add(box);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            context.BoxsTags.Add(new BoxsTags { id_box = box.id_box, id_tag = tag.id_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.DeleteBoxTag(box.id_box, tag.id_tag);

            Assert.Equal(0, await context.BoxsTags.CountAsync());
        }

        [Fact]
        public async Task DeleteBoxTag_ShouldThrowKeyNotFoundException_WhenLinkDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteBoxTag(1, 1));
        }

        [Fact]
        public async Task DeleteBoxTag_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteBoxTag(1, 1));
        }
    }
}
