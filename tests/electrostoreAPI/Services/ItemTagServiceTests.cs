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
        private ItemTagService CreateService(ApplicationDbContext context)
        {
            return new ItemTagService(_mapper, context);
        }

        private static Items BuildItem(int id, string name = "Item")
        {
            return new Items
            {
                id_item = id,
                reference_name_item = "REF-" + id,
                friendly_name_item = name
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

        private static ItemsTags BuildItemTag(int itemId, int tagId)
        {
            return new ItemsTags
            {
                id_item = itemId,
                id_tag = tagId
            };
        }

        // --- GetItemsTagsByItemId ---

        [Fact]
        public async Task GetItemsTagsByItemId_ShouldThrowKeyNotFoundException_WhenItemNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetItemsTagsByItemId(1);
            });
        }

        [Fact]
        public async Task GetItemsTagsByItemId_ShouldReturnOnlyTagsForGivenItem()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.Items.Add(BuildItem(2));
            context.Tags.Add(BuildTag(1));
            context.Tags.Add(BuildTag(2));
            context.ItemsTags.Add(BuildItemTag(1, 1));
            context.ItemsTags.Add(BuildItemTag(1, 2));
            context.ItemsTags.Add(BuildItemTag(2, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetItemsTagsByItemId(1);
            // Assert
            Assert.Equal(2, result.data.Count());
            Assert.Equal(2, result.pagination.total);
        }

        // --- GetItemsTagsByTagId ---

        [Fact]
        public async Task GetItemsTagsByTagId_ShouldThrowKeyNotFoundException_WhenTagNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetItemsTagsByTagId(1);
            });
        }

        [Fact]
        public async Task GetItemsTagsByTagId_ShouldReturnOnlyItemsForGivenTag()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.Items.Add(BuildItem(2));
            context.Tags.Add(BuildTag(1));
            context.ItemsTags.Add(BuildItemTag(1, 1));
            context.ItemsTags.Add(BuildItemTag(2, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetItemsTagsByTagId(1);
            // Assert
            Assert.Equal(2, result.data.Count());
        }

        // --- GetItemTagById ---

        [Fact]
        public async Task GetItemTagById_ShouldReturnItemTag_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.Tags.Add(BuildTag(1));
            context.ItemsTags.Add(BuildItemTag(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetItemTagById(1, 1);
            // Assert
            Assert.Equal(1, result.id_item);
            Assert.Equal(1, result.id_tag);
        }

        [Fact]
        public async Task GetItemTagById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetItemTagById(999, 999);
            });
        }

        // --- CreateItemTag ---

        [Fact]
        public async Task CreateItemTag_ShouldThrowKeyNotFoundException_WhenItemNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Tags.Add(BuildTag(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateItemTagDto { id_item = 999, id_tag = 1 };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreateItemTag(dto);
            });
        }

        [Fact]
        public async Task CreateItemTag_ShouldThrowKeyNotFoundException_WhenTagNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateItemTagDto { id_item = 1, id_tag = 999 };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreateItemTag(dto);
            });
        }

        [Fact]
        public async Task CreateItemTag_ShouldThrowInvalidOperationException_WhenAlreadyExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.Tags.Add(BuildTag(1));
            context.ItemsTags.Add(BuildItemTag(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateItemTagDto { id_item = 1, id_tag = 1 };
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.CreateItemTag(dto);
            });
        }

        [Fact]
        public async Task CreateItemTag_ShouldCreateItemTag_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.Tags.Add(BuildTag(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateItemTagDto { id_item = 1, id_tag = 1 };
            // Act
            var result = await service.CreateItemTag(dto);
            // Assert
            Assert.Equal(1, result.id_item);
            Assert.Equal(1, result.id_tag);
            Assert.Equal(1, await context.ItemsTags.CountAsync());
        }

        // --- CreateBulkItemTag ---

        [Fact]
        public async Task CreateBulkItemTag_ShouldReturnMixedResults_WhenOneItemMissing()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.Tags.Add(BuildTag(1));
            context.Tags.Add(BuildTag(2));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dtos = new List<CreateItemTagDto>
            {
                new() { id_item = 1, id_tag = 1 },
                new() { id_item = 999, id_tag = 2 }
            };
            // Act
            var result = await service.CreateBulkItemTag(dtos);
            // Assert
            Assert.Single(result.Valide);
            Assert.Single(result.Error);
        }

        // --- DeleteItemTag ---

        [Fact]
        public async Task DeleteItemTag_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeleteItemTag(999, 999);
            });
        }

        [Fact]
        public async Task DeleteItemTag_ShouldDeleteItemTag_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.Tags.Add(BuildTag(1));
            context.ItemsTags.Add(BuildItemTag(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            await service.DeleteItemTag(1, 1);
            // Assert
            Assert.Equal(0, await context.ItemsTags.CountAsync());
        }

        // --- DeleteBulkItemTag ---

        [Fact]
        public async Task DeleteBulkItemTag_ShouldReturnMixedResults_WhenOneMissing()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.Tags.Add(BuildTag(1));
            context.ItemsTags.Add(BuildItemTag(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dtos = new List<CreateItemTagDto>
            {
                new() { id_item = 1, id_tag = 1 },
                new() { id_item = 999, id_tag = 999 }
            };
            // Act
            var result = await service.DeleteBulkItemTag(dtos);
            // Assert
            Assert.Single(result.Valide);
            Assert.Single(result.Error);
            Assert.Equal(0, await context.ItemsTags.CountAsync());
        }
    }
}
