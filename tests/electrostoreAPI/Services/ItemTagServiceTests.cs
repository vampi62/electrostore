using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using AutoMapper;
using electrostore;
using electrostore.Dto;
using electrostore.Models;
using electrostore.Services.ItemTagService;
using electrostore.Tests.Utils;

namespace electrostore.Tests.Services
{
    public class ItemTagServiceTests : TestBase
    {
        [Fact]
        public async Task GetItemsTagsByItemId_ShouldReturnItemsTags_WhenItemsTagsExist()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.AddRange(new List<Items>
            {
                new Items { id_item = 1, reference_name_item = "Item1", friendly_name_item = "Item 1", seuil_min_item = 10, description_item = "Description 1" },
                new Items { id_item = 2, reference_name_item = "Item2", friendly_name_item = "Item 2", seuil_min_item = 5, description_item = "Description 2" }
            });
            context.ItemsTags.AddRange(new List<ItemsTags>
            {
                new ItemsTags { id_item = 1, id_tag = 1 },
                new ItemsTags { id_item = 1, id_tag = 2 },
                new ItemsTags { id_item = 2, id_tag = 2 }
            });
            await context.SaveChangesAsync();
            var itemId = 1;

            var itemTagService = new ItemTagService(_mapper, context);

            // Act
            var result = await itemTagService.GetItemsTagsByItemId(itemId);

            // Assert
            Assert.Contains(result, tag => tag.id_tag == 1);
            Assert.Contains(result, tag => tag.id_tag == 2);
        }

        [Fact]
        public async Task GetItemsTagsCountByItemId_ShouldReturnCorrectCount_WhenItemsTagsExist()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.AddRange(new List<Items>
            {
                new Items { id_item = 1, reference_name_item = "Item1", friendly_name_item = "Item 1", seuil_min_item = 10, description_item = "Description 1" },
                new Items { id_item = 2, reference_name_item = "Item2", friendly_name_item = "Item 2", seuil_min_item = 5, description_item = "Description 2" }
            });
            context.ItemsTags.AddRange(new List<ItemsTags>
            {
                new ItemsTags { id_item = 1, id_tag = 1 },
                new ItemsTags { id_item = 1, id_tag = 2 },
                new ItemsTags { id_item = 2, id_tag = 2 }
            });
            await context.SaveChangesAsync();
            var itemId = 1;

            var itemTagService = new ItemTagService(_mapper, context);

            // Act
            var count = await itemTagService.GetItemsTagsCountByItemId(itemId);

            // Assert
            Assert.Equal(2, count);
        }
        
        [Fact]
        public async Task GetItemsTagsByTagId_ShouldReturnItemsTags_WhenItemsTagsExist()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Tags.AddRange(new List<Tags>
            {
                new Tags { id_tag = 1, nom_tag = "Tag1", poids_tag = 1 },
                new Tags { id_tag = 2, nom_tag = "Tag2", poids_tag = 2 }
            });
            context.ItemsTags.AddRange(new List<ItemsTags>
            {
                new ItemsTags { id_item = 1, id_tag = 1 },
                new ItemsTags { id_item = 1, id_tag = 2 },
                new ItemsTags { id_item = 2, id_tag = 2 }
            });
            await context.SaveChangesAsync();
            var tagId = 2;

            var itemTagService = new ItemTagService(_mapper, context);

            // Act
            var result = await itemTagService.GetItemsTagsByTagId(tagId);

            // Assert
            Assert.Contains(result, itemTag => itemTag.id_item == 1);
            Assert.Contains(result, itemTag => itemTag.id_item == 2);
        }
        
        [Fact]
        public async Task GetItemsTagsCountByTagId_ShouldReturnCorrectCount_WhenItemsTagsExist()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Tags.AddRange(new List<Tags>
            {
                new Tags { id_tag = 1, nom_tag = "Tag1", poids_tag = 1 },
                new Tags { id_tag = 2, nom_tag = "Tag2", poids_tag = 2 }
            });
            context.ItemsTags.AddRange(new List<ItemsTags>
            {
                new ItemsTags { id_item = 1, id_tag = 1 },
                new ItemsTags { id_item = 1, id_tag = 2 },
                new ItemsTags { id_item = 2, id_tag = 2 }
            });
            await context.SaveChangesAsync();
            var tagId = 2;

            var itemTagService = new ItemTagService(_mapper, context);

            // Act
            var count = await itemTagService.GetItemsTagsCountByTagId(tagId);

            // Assert
            Assert.Equal(2, count);
        }
        
        [Fact]
        public async Task GetItemTagById_ShouldReturnItemTag_WhenItemTagExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.AddRange(new List<Items>
            {
                new Items { id_item = 1, reference_name_item = "Item1", friendly_name_item = "Item 1", seuil_min_item = 10, description_item = "Description 1" },
                new Items { id_item = 2, reference_name_item = "Item2", friendly_name_item = "Item 2", seuil_min_item = 5, description_item = "Description 2" }
            });
            context.Tags.AddRange(new List<Tags>
            {
                new Tags { id_tag = 1, nom_tag = "Tag1", poids_tag = 1 },
                new Tags { id_tag = 2, nom_tag = "Tag2", poids_tag = 2 }
            });
            context.ItemsTags.AddRange(new List<ItemsTags>
            {
                new ItemsTags { id_item = 1, id_tag = 1 },
                new ItemsTags { id_item = 1, id_tag = 2 },
                new ItemsTags { id_item = 2, id_tag = 2 }
            });
            await context.SaveChangesAsync();
            var itemId = 1;
            var tagId = 2;

            var itemTagService = new ItemTagService(_mapper, context);

            // Act
            var result = await itemTagService.GetItemTagById(itemId, tagId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(itemId, result.id_item);
            Assert.Equal(tagId, result.id_tag);
        }
        
        [Fact]
        public async Task CreateItemTag_ShouldAddItemTag_WhenItemTagIsValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.AddRange(new List<Items>
            {
                new Items { id_item = 1, reference_name_item = "Item1", friendly_name_item = "Item 1", seuil_min_item = 10, description_item = "Description 1" },
                new Items { id_item = 2, reference_name_item = "Item2", friendly_name_item = "Item 2", seuil_min_item = 5, description_item = "Description 2" }
            });
            context.Tags.AddRange(new List<Tags>
            {
                new Tags { id_tag = 1, nom_tag = "Tag1", poids_tag = 1 },
                new Tags { id_tag = 2, nom_tag = "Tag2", poids_tag = 2 }
            });
            await context.SaveChangesAsync();
            var newItemTag = new CreateItemTagDto { id_item = 1, id_tag = 2 };

            var itemTagService = new ItemTagService(_mapper, context);

            // Act
            await itemTagService.CreateItemTag(newItemTag);
            var createdItemTag = await context.ItemsTags.FindAsync(1, 2);

            // Assert
            Assert.NotNull(createdItemTag);
            Assert.Equal(newItemTag.id_item, createdItemTag.id_item);
            Assert.Equal(newItemTag.id_tag, createdItemTag.id_tag);
        }
        
        [Fact]
        public async Task CreateBulkItemTag__ShouldAddMultipleItemTags_WhenItemTagsAreValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.AddRange(new List<Items>
            {
                new Items { id_item = 1, reference_name_item = "Item1", friendly_name_item = "Item 1", seuil_min_item = 10, description_item = "Description 1" },
                new Items { id_item = 2, reference_name_item = "Item2", friendly_name_item = "Item 2", seuil_min_item = 5, description_item = "Description 2" }
            });
            context.Tags.AddRange(new List<Tags>
            {
                new Tags { id_tag = 1, nom_tag = "Tag1", poids_tag = 1 },
                new Tags { id_tag = 2, nom_tag = "Tag2", poids_tag = 2 }
            });
            await context.SaveChangesAsync();
            var newItemTags = new List<CreateItemTagDto>
            {
                new CreateItemTagDto { id_item = 1, id_tag = 1 },
                new CreateItemTagDto { id_item = 2, id_tag = 2 }
            };

            var itemTagService = new ItemTagService(_mapper, context);
            
            // Act
            await itemTagService.CreateBulkItemTag(newItemTags);
            var createdItemTag1 = await context.ItemsTags.FindAsync(1, 1);
            var createdItemTag2 = await context.ItemsTags.FindAsync(2, 2);
            // Assert
            Assert.NotNull(createdItemTag1);
            Assert.NotNull(createdItemTag2);
            Assert.Equal(1, createdItemTag1.id_item);
            Assert.Equal(1, createdItemTag1.id_tag);
            Assert.Equal(2, createdItemTag2.id_item);
            Assert.Equal(2, createdItemTag2.id_tag);
        }
        
        [Fact]
        public async Task DeleteItemTag_ShouldRemoveItemTag_WhenItemTagExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.ItemsTags.AddRange(new List<ItemsTags>
            {
                new ItemsTags { id_item = 1, id_tag = 1 },
                new ItemsTags { id_item = 1, id_tag = 2 }
            });
            await context.SaveChangesAsync();
            var itemId = 1;
            var tagId = 2;

            var itemTagService = new ItemTagService(_mapper, context);

            // Act
            await itemTagService.DeleteItemTag(itemId, tagId);
            var deletedItemTag = await context.ItemsTags.FindAsync(itemId, tagId);

            // Assert
            Assert.Null(deletedItemTag);
        }
        
        [Fact]
        public async Task DeleteBulkItemTag_ShouldRemoveMultipleItemTags_WhenItemTagsExist()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.ItemsTags.AddRange(new List<ItemsTags>
            {
                new ItemsTags { id_item = 1, id_tag = 1 },
                new ItemsTags { id_item = 2, id_tag = 2 }
            });
            await context.SaveChangesAsync();
            var itemTagsToDelete = new List<CreateItemTagDto>
            {
                new CreateItemTagDto { id_item = 1, id_tag = 1 },
                new CreateItemTagDto { id_item = 2, id_tag = 2 }
            };

            var itemTagService = new ItemTagService(_mapper, context);

            // Act
            await itemTagService.DeleteBulkItemTag(itemTagsToDelete);
            var deletedItemTag1 = await context.ItemsTags.FindAsync(1, 1);
            var deletedItemTag2 = await context.ItemsTags.FindAsync(2, 2);

            // Assert
            Assert.Null(deletedItemTag1);
            Assert.Null(deletedItemTag2);
        }
    }
}