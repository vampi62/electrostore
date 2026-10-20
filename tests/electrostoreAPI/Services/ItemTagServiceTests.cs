using Microsoft.EntityFrameworkCore;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.ItemTagService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class ItemTagServiceTests : TestBase
    {
        private ItemTagService CreateService(ApplicationDbContext context) =>
            new(_mapper, context);

        private static Items BuildItem(string reference = "item") => new()
        {
            reference_name_item = reference,
            friendly_name_item = reference,
            threshold_min_item = 0
        };

        private static Tags BuildTag(string name = "tag") => new() { name_tag = name };

        // --- GetItemsTagsByItemId ---

        [Fact]
        public async Task GetItemsTagsByItemId_ShouldReturnTagsLinkedToItem()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem();
            var tag = BuildTag();
            context.Items.Add(item);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            context.ItemsTags.Add(new ItemsTags { id_item = item.id_item, id_tag = tag.id_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetItemsTagsByItemId(item.id_item);

            var single = Assert.Single(result.data);
            Assert.Equal(tag.id_tag, single.id_tag);
        }

        [Fact]
        public async Task GetItemsTagsByItemId_ShouldThrowKeyNotFoundException_WhenItemDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetItemsTagsByItemId(999));
        }

        // --- GetItemsTagsByTagId ---

        [Fact]
        public async Task GetItemsTagsByTagId_ShouldReturnItemsLinkedToTag()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem();
            var tag = BuildTag();
            context.Items.Add(item);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            context.ItemsTags.Add(new ItemsTags { id_item = item.id_item, id_tag = tag.id_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetItemsTagsByTagId(tag.id_tag);

            var single = Assert.Single(result.data);
            Assert.Equal(item.id_item, single.id_item);
        }

        [Fact]
        public async Task GetItemsTagsByTagId_ShouldThrowKeyNotFoundException_WhenTagDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetItemsTagsByTagId(999));
        }

        // --- GetItemTagById ---

        [Fact]
        public async Task GetItemTagById_ShouldReturnItemTag_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem();
            var tag = BuildTag();
            context.Items.Add(item);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            context.ItemsTags.Add(new ItemsTags { id_item = item.id_item, id_tag = tag.id_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetItemTagById(item.id_item, tag.id_tag);

            Assert.Equal(item.id_item, result.id_item);
            Assert.Equal(tag.id_tag, result.id_tag);
        }

        [Fact]
        public async Task GetItemTagById_ShouldThrowKeyNotFoundException_WhenLinkDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetItemTagById(1, 1));
        }

        // --- CreateItemTag ---

        [Fact]
        public async Task CreateItemTag_ShouldPersistLink_WhenItemAndTagExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem();
            var tag = BuildTag();
            context.Items.Add(item);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateItemTagDto { id_item = item.id_item, id_tag = tag.id_tag };

            var result = await service.CreateItemTag(dto);

            Assert.Equal(item.id_item, result.id_item);
            Assert.Equal(1, await context.ItemsTags.CountAsync());
        }

        [Fact]
        public async Task CreateItemTag_ShouldThrowKeyNotFoundException_WhenItemDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tag = BuildTag();
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateItemTagDto { id_item = 999, id_tag = tag.id_tag };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateItemTag(dto));
        }

        [Fact]
        public async Task CreateItemTag_ShouldThrowKeyNotFoundException_WhenTagDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem();
            context.Items.Add(item);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateItemTagDto { id_item = item.id_item, id_tag = 999 };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateItemTag(dto));
        }

        [Fact]
        public async Task CreateItemTag_ShouldThrowInvalidOperationException_WhenLinkAlreadyExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem();
            var tag = BuildTag();
            context.Items.Add(item);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            context.ItemsTags.Add(new ItemsTags { id_item = item.id_item, id_tag = tag.id_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateItemTagDto { id_item = item.id_item, id_tag = tag.id_tag };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateItemTag(dto));
        }

        // --- DeleteItemTag ---

        [Fact]
        public async Task DeleteItemTag_ShouldRemoveLink_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem();
            var tag = BuildTag();
            context.Items.Add(item);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            context.ItemsTags.Add(new ItemsTags { id_item = item.id_item, id_tag = tag.id_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.DeleteItemTag(item.id_item, tag.id_tag);

            Assert.Equal(0, await context.ItemsTags.CountAsync());
        }

        [Fact]
        public async Task DeleteItemTag_ShouldThrowKeyNotFoundException_WhenLinkDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteItemTag(1, 1));
        }

        // --- expand / bulk operations ---

        private async Task<(int parentId, int tag1Id, int tag2Id)> SeedForBulk(ApplicationDbContext context)
        {
            var parent = BuildItem();
            context.Items.Add(parent);
            await context.SaveChangesAsync();
            var tag1 = BuildTag("bulk-1");
            var tag2 = BuildTag("bulk-2");
            context.Tags.AddRange(tag1, tag2);
            await context.SaveChangesAsync();
            return (parent.id_item, tag1.id_tag, tag2.id_tag);
        }

        [Fact]
        public async Task GetItemsTagsByItemId_ShouldExpandParentAndTag_WhenRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parentId, tagId, _) = await SeedForBulk(context);
            context.ItemsTags.Add(new ItemsTags { id_item = parentId, id_tag = tagId });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetItemsTagsByItemId(parentId, expand: ["item", "tag"]);

            var link = Assert.Single(result.data);
            Assert.NotNull(link.item);
            Assert.NotNull(link.tag);
        }

        [Fact]
        public async Task GetItemsTagsByTagId_ShouldExpandParentAndTag_WhenRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parentId, tagId, _) = await SeedForBulk(context);
            context.ItemsTags.Add(new ItemsTags { id_item = parentId, id_tag = tagId });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetItemsTagsByTagId(tagId, expand: ["item", "tag"]);

            var link = Assert.Single(result.data);
            Assert.NotNull(link.item);
            Assert.NotNull(link.tag);
        }

        [Fact]
        public async Task GetItemTagById_ShouldExpandParentAndTag_WhenRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parentId, tagId, _) = await SeedForBulk(context);
            context.ItemsTags.Add(new ItemsTags { id_item = parentId, id_tag = tagId });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetItemTagById(parentId, tagId, ["item", "tag"]);

            Assert.NotNull(result.item);
            Assert.NotNull(result.tag);
        }

        [Fact]
        public async Task CreateBulkItemTag_ShouldReturnValidAndErrorEntries()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parentId, tag1Id, _) = await SeedForBulk(context);
            var service = CreateService(context);

            var result = await service.CreateBulkItemTag(
            [
                new CreateItemTagDto { id_item = parentId, id_tag = tag1Id },
                new CreateItemTagDto { id_item = 9999, id_tag = tag1Id },
                new CreateItemTagDto { id_item = parentId, id_tag = tag1Id }
            ]);

            Assert.Single(result.valide);
            Assert.Equal(2, result.error.Count());
            Assert.Equal(1, await context.ItemsTags.CountAsync());
        }

        [Fact]
        public async Task DeleteBulkItemTag_ShouldReturnValidAndErrorEntries()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parentId, tag1Id, tag2Id) = await SeedForBulk(context);
            context.ItemsTags.Add(new ItemsTags { id_item = parentId, id_tag = tag1Id });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.DeleteBulkItemTag(
            [
                new CreateItemTagDto { id_item = parentId, id_tag = tag1Id },
                new CreateItemTagDto { id_item = parentId, id_tag = tag2Id }
            ]);

            var deleted = Assert.Single(result.valide);
            Assert.Equal(tag1Id, deleted.id_tag);
            Assert.Single(result.error);
            Assert.Equal(0, await context.ItemsTags.CountAsync());
        }
    }
}
