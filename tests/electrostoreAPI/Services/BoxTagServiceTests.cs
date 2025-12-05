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
using electrostore.Enums;
using electrostore.Models;
using electrostore.Services.BoxTagService;
using electrostore.Services.SessionService;
using electrostore.Tests.Utils;

namespace electrostore.Tests.Services
{
    public class BoxTagServiceTests : TestBase
    {
        private readonly Mock<ISessionService> _sessionService;
        public BoxTagServiceTests()
        {
            _sessionService = new Mock<ISessionService>();
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Admin);
        }
        
        [Fact]
        public async Task GetBoxsTagsByBoxId_ShouldReturnBoxsTags_WhenBoxsTagsExist()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Boxs.AddRange(new List<Boxs>
            {
                new Boxs { id_box = 1, id_store = 1, xstart_box = 0, ystart_box = 0, xend_box = 10, yend_box = 10 },
                new Boxs { id_box = 2, id_store = 1, xstart_box = 10, ystart_box = 10, xend_box = 20, yend_box = 20 }
            });
            context.BoxsTags.AddRange(new List<BoxsTags>
            {
                new BoxsTags { id_box = 1, id_tag = 1 },
                new BoxsTags { id_box = 1, id_tag = 2 },
                new BoxsTags { id_box = 2, id_tag = 1 }
            });
            await context.SaveChangesAsync();
            var boxTagService = new BoxTagService(_mapper, context, _sessionService.Object);
            // Act
            var result = await boxTagService.GetBoxsTagsByBoxId(1);
            // Assert
            Assert.Contains(result, bt => bt.id_tag == 1);
            Assert.Contains(result, bt => bt.id_tag == 2);
        }

        [Fact]
        public async Task GetBoxsTagsCountByBoxId_ShouldReturnCorrectCount()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Boxs.AddRange(new List<Boxs>
            {
                new Boxs { id_box = 1, id_store = 1, xstart_box = 0, ystart_box = 0, xend_box = 10, yend_box = 10 },
                new Boxs { id_box = 2, id_store = 1, xstart_box = 10, ystart_box = 10, xend_box = 20, yend_box = 20 }
            });
            context.BoxsTags.AddRange(new List<BoxsTags>
            {
                new BoxsTags { id_box = 1, id_tag = 1 },
                new BoxsTags { id_box = 1, id_tag = 2 },
                new BoxsTags { id_box = 2, id_tag = 1 }
            });
            await context.SaveChangesAsync();
            var boxTagService = new BoxTagService(_mapper, context, _sessionService.Object);
            // Act
            var count = await boxTagService.GetBoxsTagsCountByBoxId(1);
            // Assert
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task GetBoxsTagsByTagId_ShouldReturnBoxsTags_WhenBoxsTagsExist()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Tags.AddRange(new List<Tags>
            {
                new Tags { id_tag = 1, nom_tag = "Tag1", poids_tag = 1 },
                new Tags { id_tag = 2, nom_tag = "Tag2", poids_tag = 2 }
            });
            context.BoxsTags.AddRange(new List<BoxsTags>
            {
                new BoxsTags { id_box = 1, id_tag = 1 },
                new BoxsTags { id_box = 1, id_tag = 2 },
                new BoxsTags { id_box = 2, id_tag = 1 }
            });
            await context.SaveChangesAsync();
            var boxTagService = new BoxTagService(_mapper, context, _sessionService.Object);
            // Act
            var result = await boxTagService.GetBoxsTagsByTagId(1);
            // Assert
            Assert.Contains(result, bt => bt.id_box == 1);
            Assert.Contains(result, bt => bt.id_box == 2);
        }

        [Fact]
        public async Task GetBoxsTagsCountByTagId_ShouldReturnCorrectCount()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Tags.AddRange(new List<Tags>
            {
                new Tags { id_tag = 1, nom_tag = "Tag1", poids_tag = 1 },
                new Tags { id_tag = 2, nom_tag = "Tag2", poids_tag = 2 }
            });
            context.BoxsTags.AddRange(new List<BoxsTags>
            {
                new BoxsTags { id_box = 1, id_tag = 1 },
                new BoxsTags { id_box = 1, id_tag = 2 },
                new BoxsTags { id_box = 2, id_tag = 1 }
            });
            await context.SaveChangesAsync();
            var boxTagService = new BoxTagService(_mapper, context, _sessionService.Object);
            // Act
            var count = await boxTagService.GetBoxsTagsCountByTagId(1);
            // Assert
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task GetBoxTagById_ShouldReturnBoxTag_WhenBoxTagExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Boxs.AddRange(new List<Boxs>
            {
                new Boxs { id_box = 1, id_store = 1, xstart_box = 0, ystart_box = 0, xend_box = 10, yend_box = 10 },
                new Boxs { id_box = 2, id_store = 1, xstart_box = 10, ystart_box = 10, xend_box = 20, yend_box = 20 }
            });
            context.Tags.AddRange(new List<Tags>
            {
                new Tags { id_tag = 1, nom_tag = "Tag1", poids_tag = 1 },
                new Tags { id_tag = 2, nom_tag = "Tag2", poids_tag = 2 }
            });
            context.BoxsTags.AddRange(new List<BoxsTags>
            {
                new BoxsTags { id_box = 1, id_tag = 1 },
                new BoxsTags { id_box = 1, id_tag = 2 }
            });
            await context.SaveChangesAsync();
            var boxTagService = new BoxTagService(_mapper, context, _sessionService.Object);
            // Act
            var result = await boxTagService.GetBoxTagById(1, 2);
            // Assert
            Assert.Equal(1, result.id_box);
            Assert.Equal(2, result.id_tag);
        }

        [Fact]
        public async Task CreateBoxTag_ShouldAddBoxTag()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Boxs.AddRange(new List<Boxs>
            {
                new Boxs { id_box = 1, id_store = 1, xstart_box = 0, ystart_box = 0, xend_box = 10, yend_box = 10 },
                new Boxs { id_box = 2, id_store = 1, xstart_box = 10, ystart_box = 10, xend_box = 20, yend_box = 20 }
            });
            context.Tags.AddRange(new List<Tags>
            {
                new Tags { id_tag = 1, nom_tag = "Tag1", poids_tag = 1 },
                new Tags { id_tag = 2, nom_tag = "Tag2", poids_tag = 2 }
            });
            await context.SaveChangesAsync();
            var newBoxTag = new CreateBoxTagDto { id_box = 1, id_tag = 2 };
            var boxTagService = new BoxTagService(_mapper, context, _sessionService.Object);
            // Act
            await boxTagService.CreateBoxTag(newBoxTag);
            var createdBoxTag = await context.BoxsTags.FindAsync(1, 2);
            // Assert
            Assert.NotNull(createdBoxTag);
            Assert.Equal(1, createdBoxTag.id_box);
            Assert.Equal(2, createdBoxTag.id_tag);
        }

        [Fact]
        public async Task CreateBulkBoxTag_ShouldAddBoxTags()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Boxs.AddRange(new List<Boxs>
            {
                new Boxs { id_box = 1, id_store = 1, xstart_box = 0, ystart_box = 0, xend_box = 10, yend_box = 10 },
                new Boxs { id_box = 2, id_store = 1, xstart_box = 10, ystart_box = 10, xend_box = 20, yend_box = 20 }
            });
            context.Tags.AddRange(new List<Tags>
            {
                new Tags { id_tag = 1, nom_tag = "Tag1", poids_tag = 1 },
                new Tags { id_tag = 2, nom_tag = "Tag2", poids_tag = 2 }
            });
            var newBoxTags = new List<CreateBoxTagDto>
            {
                new CreateBoxTagDto { id_box = 1, id_tag = 1 },
                new CreateBoxTagDto { id_box = 2, id_tag = 2 }
            };
            await context.SaveChangesAsync();
            var boxTagService = new BoxTagService(_mapper, context, _sessionService.Object);
            // Act
            await boxTagService.CreateBulkBoxTag(newBoxTags);
            var createdBoxTag1 = await context.BoxsTags.FindAsync(1, 1);
            var createdBoxTag2 = await context.BoxsTags.FindAsync(2, 2);
            // Assert
            Assert.NotNull(createdBoxTag1);
            Assert.Equal(1, createdBoxTag1.id_box);
            Assert.Equal(1, createdBoxTag1.id_tag);
            Assert.NotNull(createdBoxTag2);
            Assert.Equal(2, createdBoxTag2.id_box);
            Assert.Equal(2, createdBoxTag2.id_tag);
        }

        [Fact]
        public async Task DeleteBoxTag_ShouldRemoveBoxTag()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.BoxsTags.AddRange(new List<BoxsTags>
            {
                new BoxsTags { id_box = 1, id_tag = 1 },
                new BoxsTags { id_box = 1, id_tag = 2 }
            });
            await context.SaveChangesAsync();
            var boxTagService = new BoxTagService(_mapper, context, _sessionService.Object);
            // Act
            await boxTagService.DeleteBoxTag(1, 2);
            var deletedBoxTag = await context.BoxsTags.FindAsync(1, 2);
            // Assert
            Assert.Null(deletedBoxTag);
        }

        [Fact]
        public async Task DeleteBulkBoxTag_ShouldRemoveBoxTags()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.BoxsTags.AddRange(new List<BoxsTags>
            {
                new BoxsTags { id_box = 1, id_tag = 1 },
                new BoxsTags { id_box = 2, id_tag = 2 }
            });
            await context.SaveChangesAsync();
            var boxTagsToDelete = new List<CreateBoxTagDto>
            {
                new CreateBoxTagDto { id_box = 1, id_tag = 1 },
                new CreateBoxTagDto { id_box = 2, id_tag = 2 }
            };
            var boxTagService = new BoxTagService(_mapper, context, _sessionService.Object);
            // Act
            await boxTagService.DeleteBulkBoxTag(boxTagsToDelete);
            var deletedBoxTag1 = await context.BoxsTags.FindAsync(1, 1);
            var deletedBoxTag2 = await context.BoxsTags.FindAsync(2, 2);
            // Assert
            Assert.Null(deletedBoxTag1);
            Assert.Null(deletedBoxTag2);
        }

        [Fact]
        public async Task CheckIfStoreExists_ShouldNotThrowException_WhenBoxBelongsToStore()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Boxs.AddRange(new List<Boxs>
            {
                new Boxs { id_box = 1, id_store = 1, xstart_box = 0, ystart_box = 0, xend_box = 10, yend_box = 10 },
                new Boxs { id_box = 2, id_store = 1, xstart_box = 10, ystart_box = 10, xend_box = 20, yend_box = 20 }
            });
            context.Stores.AddRange(new List<Stores>
            {
                new Stores { id_store = 1, nom_store = "Store1", xlength_store=100, ylength_store=100, mqtt_name_store="store1/mqtt" }
            });
            await context.SaveChangesAsync();
            var boxTagService = new BoxTagService(_mapper, context, _sessionService.Object);
            // Act & Assert
            await boxTagService.CheckIfStoreExists(1, 2);
        }
    }
}