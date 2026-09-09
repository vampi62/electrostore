using Microsoft.EntityFrameworkCore;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.TagService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class TagServiceTests : TestBase
    {
        private TagService CreateService(ApplicationDbContext context) =>
            new(_mapper, context);

        private static Tags BuildTag(string name = "tag", int weight = 0) => new()
        {
            name_tag = name,
            weight_tag = weight
        };

        // --- GetTags ---

        [Fact]
        public async Task GetTags_ShouldReturnAllTags_WhenNoFilterApplied()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Tags.AddRange(BuildTag("a"), BuildTag("b"));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetTags();

            Assert.Equal(2, result.pagination.total);
            Assert.Equal(2, result.data.Count());
        }

        // --- GetTagById ---

        [Fact]
        public async Task GetTagById_ShouldReturnTag_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tag = BuildTag();
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetTagById(tag.id_tag);

            Assert.Equal(tag.id_tag, result.id_tag);
            Assert.Equal(0, result.items_tags_count);
        }

        [Fact]
        public async Task GetTagById_ShouldThrowKeyNotFoundException_WhenTagDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetTagById(999));
        }

        // --- CreateTag ---

        [Fact]
        public async Task CreateTag_ShouldPersistTag()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateTagDto { name_tag = "new-tag", weight_tag = 5 };

            var result = await service.CreateTag(dto);

            Assert.Equal("new-tag", result.name_tag);
            Assert.Equal(1, await context.Tags.CountAsync());
        }

        [Fact]
        public async Task CreateTag_ShouldThrowInvalidOperationException_WhenNameAlreadyExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Tags.Add(BuildTag("dup"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateTagDto { name_tag = "dup", weight_tag = 5 };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateTag(dto));
        }

        // --- UpdateTag ---

        [Fact]
        public async Task UpdateTag_ShouldUpdateProvidedFields()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tag = BuildTag("a", 1);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new UpdateTagDto { name_tag = "renamed", weight_tag = 9 };

            var result = await service.UpdateTag(tag.id_tag, dto);

            Assert.Equal("renamed", result.name_tag);
            Assert.Equal(9, result.weight_tag);
        }

        [Fact]
        public async Task UpdateTag_ShouldThrowInvalidOperationException_WhenNewNameAlreadyUsedByAnotherTag()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tag1 = BuildTag("a");
            var tag2 = BuildTag("b");
            context.Tags.AddRange(tag1, tag2);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new UpdateTagDto { name_tag = "b" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateTag(tag1.id_tag, dto));
        }

        [Fact]
        public async Task UpdateTag_ShouldThrowKeyNotFoundException_WhenTagDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateTag(999, new UpdateTagDto()));
        }

        // --- DeleteTag ---

        [Fact]
        public async Task DeleteTag_ShouldRemoveTag_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tag = BuildTag();
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.DeleteTag(tag.id_tag);

            Assert.Equal(0, await context.Tags.CountAsync());
        }

        [Fact]
        public async Task DeleteTag_ShouldThrowKeyNotFoundException_WhenTagDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteTag(999));
        }
    }
}
