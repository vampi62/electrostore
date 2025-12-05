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
using electrostore.Services.StoreTagService;
using electrostore.Services.SessionService;
using electrostore.Tests.Utils;

namespace electrostore.Tests.Services
{
    public class StoreTagServiceTests : TestBase
    {
        private readonly Mock<ISessionService> _sessionService;
        public StoreTagServiceTests()
        {
            _sessionService = new Mock<ISessionService>();
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Admin);
        }

        [Fact]
        public async Task GetStoresTagsByStoreId_ShouldReturnStoresTags_WhenStoresTagsExist()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.AddRange(new List<Stores>
            {
                new Stores { id_store = 1, nom_store = "Store1", xlength_store=100, ylength_store=100, mqtt_name_store="store1/mqtt" }
            });
            context.StoresTags.AddRange(new List<StoresTags>
            {
                new StoresTags { id_store = 1, id_tag = 1 },
                new StoresTags { id_store = 1, id_tag = 2 },
                new StoresTags { id_store = 2, id_tag = 1 }
            });
            await context.SaveChangesAsync();
            var storeTagService = new StoreTagService(_mapper, context, _sessionService.Object);
            // Act
            var result = await storeTagService.GetStoresTagsByStoreId(1);
            // Assert
            Assert.Contains(result, st => st.id_tag == 1);
            Assert.Contains(result, st => st.id_tag == 2);
        }

        [Fact]
        public async Task GetStoresTagsCountByStoreId_ShouldReturnCorrectCount_WhenStoresTagsExist()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.AddRange(new List<Stores>
            {
                new Stores { id_store = 1, nom_store = "Store1", xlength_store=100, ylength_store=100, mqtt_name_store="store1/mqtt" }
            });
            context.StoresTags.AddRange(new List<StoresTags>
            {
                new StoresTags { id_store = 1, id_tag = 1 },
                new StoresTags { id_store = 1, id_tag = 2 },
                new StoresTags { id_store = 2, id_tag = 1 }
            });
            await context.SaveChangesAsync();
            var storeTagService = new StoreTagService(_mapper, context, _sessionService.Object);
            // Act
            var result = await storeTagService.GetStoresTagsCountByStoreId(1);
            // Assert
            Assert.Equal(2, result);
        }

        [Fact]
        public async Task GetStoresTagsByTagId_ShouldReturnStoresTags_WhenStoresTagsExist()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Tags.AddRange(new List<Tags>
            {
                new Tags { id_tag = 1, nom_tag = "Tag1", poids_tag = 1 },
                new Tags { id_tag = 2, nom_tag = "Tag2", poids_tag = 2 }
            });
            context.StoresTags.AddRange(new List<StoresTags>
            {
                new StoresTags { id_store = 1, id_tag = 1 },
                new StoresTags { id_store = 2, id_tag = 1 },
                new StoresTags { id_store = 1, id_tag = 2 }
            });
            await context.SaveChangesAsync();
            var storeTagService = new StoreTagService(_mapper, context, _sessionService.Object);
            // Act
            var result = await storeTagService.GetStoresTagsByTagId(1);
            // Assert
            Assert.Contains(result, st => st.id_store == 1);
            Assert.Contains(result, st => st.id_store == 2);
        }

        [Fact]
        public async Task GetStoresTagsCountByTagId_ShouldReturnCorrectCount_WhenStoresTagsExist()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Tags.AddRange(new List<Tags>
            {
                new Tags { id_tag = 1, nom_tag = "Tag1", poids_tag = 1 },
                new Tags { id_tag = 2, nom_tag = "Tag2", poids_tag = 2 }
            });
            context.StoresTags.AddRange(new List<StoresTags>
            {
                new StoresTags { id_store = 1, id_tag = 1 },
                new StoresTags { id_store = 2, id_tag = 1 },
                new StoresTags { id_store = 1, id_tag = 2 }
            });
            await context.SaveChangesAsync();
            var storeTagService = new StoreTagService(_mapper, context, _sessionService.Object);
            // Act
            var result = await storeTagService.GetStoresTagsCountByTagId(1);
            // Assert
            Assert.Equal(2, result);
        }

        [Fact]
        public async Task GetStoreTagById_ShouldReturnStoreTag_WhenStoreTagExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.AddRange(new List<Stores>
            {
                new Stores { id_store = 1, nom_store = "Store1", xlength_store=100, ylength_store=100, mqtt_name_store="store1/mqtt" }
            });
            context.Tags.AddRange(new List<Tags>
            {
                new Tags { id_tag = 1, nom_tag = "Tag1", poids_tag = 1 }
            });
            context.StoresTags.AddRange(new List<StoresTags>
            {
                new StoresTags { id_store = 1, id_tag = 1 }
            });
            await context.SaveChangesAsync();
            var storeTagService = new StoreTagService(_mapper, context, _sessionService.Object);
            // Act
            var result = await storeTagService.GetStoreTagById(1, 1);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.id_store);
            Assert.Equal(1, result.id_tag);
        }

        [Fact]
        public async Task CreateStoreTag_ShouldAddStoreTag_WhenDataIsValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.AddRange(new List<Stores>
            {
                new Stores { id_store = 1, nom_store = "Store1", xlength_store=100, ylength_store=100, mqtt_name_store="store1/mqtt" }
            });
            context.Tags.AddRange(new List<Tags>
            {
                new Tags { id_tag = 1, nom_tag = "Tag1", poids_tag = 1 },
                new Tags { id_tag = 2, nom_tag = "Tag2", poids_tag = 2 }
            });
            await context.SaveChangesAsync();
            var newStoreTag = new CreateStoreTagDto { id_store = 1, id_tag = 2 };
            var storeTagService = new StoreTagService(_mapper, context, _sessionService.Object);
            // Act
            var result = await storeTagService.CreateStoreTag(newStoreTag);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.id_store);
            Assert.Equal(2, result.id_tag);
        }

        [Fact]
        public async Task CreateBulkStoreTag_ShouldAddStoreTags_WhenDataIsValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.AddRange(new List<Stores>
            {
                new Stores { id_store = 1, nom_store = "Store1", xlength_store=100, ylength_store=100, mqtt_name_store="store1/mqtt" },
                new Stores { id_store = 2, nom_store = "Store2", xlength_store=200, ylength_store=200, mqtt_name_store="store2/mqtt" }
            });
            context.Tags.AddRange(new List<Tags>
            {
                new Tags { id_tag = 1, nom_tag = "Tag1", poids_tag = 1 },
                new Tags { id_tag = 2, nom_tag = "Tag2", poids_tag = 2 }
            });
            await context.SaveChangesAsync();
            var newStoreTags = new List<CreateStoreTagDto>
            {
                new CreateStoreTagDto { id_store = 1, id_tag = 1 },
                new CreateStoreTagDto { id_store = 2, id_tag = 2 }
            };
            var storeTagService = new StoreTagService(_mapper, context, _sessionService.Object);
            // Act
            await storeTagService.CreateBulkStoreTag(newStoreTags);
            // Assert
            var storeTagsInDb = await context.StoresTags.ToListAsync();
            Assert.Equal(2, storeTagsInDb.Count);
        }

        [Fact]
        public async Task DeleteStoreTag_ShouldRemoveStoreTag_WhenStoreTagExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.AddRange(new List<Stores>
            {
                new Stores { id_store = 1, nom_store = "Store1", xlength_store=100, ylength_store=100, mqtt_name_store="store1/mqtt" }
            });
            context.Tags.AddRange(new List<Tags>
            {
                new Tags { id_tag = 1, nom_tag = "Tag1", poids_tag = 1 }
            });
            context.StoresTags.AddRange(new List<StoresTags>
            {
                new StoresTags { id_store = 1, id_tag = 1 }
            });
            await context.SaveChangesAsync();
            var storeTagService = new StoreTagService(_mapper, context, _sessionService.Object);
            // Act
            await storeTagService.DeleteStoreTag(1, 1);
            // Assert
            var storeTagInDb = await context.StoresTags.FindAsync(1, 1);
            Assert.Null(storeTagInDb);
        }

        [Fact]
        public async Task DeleteBulkStoreTag_ShouldRemoveStoreTags_WhenStoreTagsExist()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Stores.AddRange(new List<Stores>
            {
                new Stores { id_store = 1, nom_store = "Store1", xlength_store=100, ylength_store=100, mqtt_name_store="store1/mqtt" },
                new Stores { id_store = 2, nom_store = "Store2", xlength_store=200, ylength_store=200, mqtt_name_store="store2/mqtt" }
            });
            context.Tags.AddRange(new List<Tags>
            {
                new Tags { id_tag = 1, nom_tag = "Tag1", poids_tag = 1 },
                new Tags { id_tag = 2, nom_tag = "Tag2", poids_tag = 2 }
            });
            context.StoresTags.AddRange(new List<StoresTags>
            {
                new StoresTags { id_store = 1, id_tag = 1 },
                new StoresTags { id_store = 2, id_tag = 2 }
            });
            await context.SaveChangesAsync();
            var storeTagsToDelete = new List<CreateStoreTagDto>
            {
                new CreateStoreTagDto { id_store = 1, id_tag = 1 },
                new CreateStoreTagDto { id_store = 2, id_tag = 2 }
            };
            var storeTagService = new StoreTagService(_mapper, context, _sessionService.Object);
            // Act
            await storeTagService.DeleteBulkStoreTag(storeTagsToDelete);
            // Assert
            var storeTagsInDb = await context.StoresTags.ToListAsync();
            Assert.Empty(storeTagsInDb);
        }
    }
}