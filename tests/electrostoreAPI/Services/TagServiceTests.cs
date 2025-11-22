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
using electrostore.Services.TagService;
using electrostore.Tests.Utils;

namespace electrostore.Tests.Services
{
    public class TagServiceTests : TestBase
    {
        [Fact]
        public async Task GetTags_ShouldReturnTags_WhenTagsExist()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Tags.AddRange(new List<Tags>
            {
                new Tags { id_tag = 1, nom_tag = "Tag1", poids_tag = 1 },
                new Tags { id_tag = 2, nom_tag = "Tag2", poids_tag = 2 }
            });
            await context.SaveChangesAsync();
            var tagService = new TagService(_mapper, context);
            // Act
            var result = await tagService.GetTags();
            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, ((List<ReadExtendedTagDto>)result).Count);
        }

        [Fact]
        public async Task GetTagsCount_ShouldReturnCorrectCount()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Tags.AddRange(new List<Tags>
            {
                new Tags { id_tag = 1, nom_tag = "Tag1", poids_tag = 1 },
                new Tags { id_tag = 2, nom_tag = "Tag2", poids_tag = 2 },
                new Tags { id_tag = 3, nom_tag = "Tag3", poids_tag = 3 }
            });
            await context.SaveChangesAsync();
            var tagService = new TagService(_mapper, context);
            // Act
            var count = await tagService.GetTagsCount();
            // Assert
            Assert.Equal(3, count);
        }

        [Fact]
        public async Task GetTagById_ShouldReturnTag_WhenTagExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Tags.Add(new Tags { id_tag = 1, nom_tag = "Tag1", poids_tag = 1 });
            await context.SaveChangesAsync();
            var tagService = new TagService(_mapper, context);
            // Act
            var result = await tagService.GetTagById(1);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.id_tag);
            Assert.Equal("Tag1", result.nom_tag);
        }

        [Fact]
        public async Task CreateTag_ShouldAddTag_WhenTagIsValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tagService = new TagService(_mapper, context);
            var newTagDto = new CreateTagDto { nom_tag = "NewTag", poids_tag = 5 };
            // Act
            var result = await tagService.CreateTag(newTagDto);
            // Assert
            Assert.NotNull(result);
            Assert.Equal("NewTag", result.nom_tag);
            Assert.Equal(5, result.poids_tag);
            var tagInDb = await context.Tags.FindAsync(result.id_tag);
            Assert.NotNull(tagInDb);
        }

        [Fact]
        public async Task UpdateTag_ShouldModifyTag_WhenTagExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Tags.Add(new Tags { id_tag = 1, nom_tag = "OldTag", poids_tag = 2 });
            await context.SaveChangesAsync();
            var tagService = new TagService(_mapper, context);
            var updateTagDto = new UpdateTagDto { nom_tag = "UpdatedTag", poids_tag = 10 };
            // Act
            var result = await tagService.UpdateTag(1, updateTagDto);
            // Assert
            Assert.NotNull(result);
            Assert.Equal("UpdatedTag", result.nom_tag);
            Assert.Equal(10, result.poids_tag);
            var tagInDb = await context.Tags.FindAsync(1);
            Assert.Equal("UpdatedTag", tagInDb.nom_tag);
            Assert.Equal(10, tagInDb.poids_tag);
        }

        [Fact]
        public async Task DeleteTag_ShouldRemoveTag_WhenTagExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Tags.Add(new Tags { id_tag = 1, nom_tag = "TagToDelete", poids_tag = 3 });
            await context.SaveChangesAsync();
            var tagService = new TagService(_mapper, context);
            // Act
            await tagService.DeleteTag(1);
            // Assert
            var tagInDb = await context.Tags.FindAsync(1);
            Assert.Null(tagInDb);
        }

        [Fact]
        public async Task CreateBulkTag_ShouldAddMultipleTags_WhenTagsAreValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tagService = new TagService(_mapper, context);
            var newTagsDto = new List<CreateTagDto>
            {
                new CreateTagDto { nom_tag = "BulkTag1", poids_tag = 1 },
                new CreateTagDto { nom_tag = "BulkTag2", poids_tag = 2 }
            };
            // Act
            var result = await tagService.CreateBulkTag(newTagsDto);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Valide.Count);
            var tagInDb1 = await context.Tags.FirstOrDefaultAsync(t => t.nom_tag == "BulkTag1");
            var tagInDb2 = await context.Tags.FirstOrDefaultAsync(t => t.nom_tag == "BulkTag2");
            Assert.NotNull(tagInDb1);
            Assert.NotNull(tagInDb2);
        }
    }
}