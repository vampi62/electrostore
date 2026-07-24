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
        private TagService CreateService(ApplicationDbContext context)
        {
            return new TagService(_mapper, context);
        }

        private static Tags BuildTag(int id, string name, int weight = 0)
        {
            return new Tags
            {
                id_tag = id,
                nom_tag = name,
                poids_tag = weight
            };
        }

        // --- GetTags ---

        [Fact]
        public async Task GetTags_ShouldReturnAll_Paginated()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Tags.Add(BuildTag(1, "Fragile"));
            context.Tags.Add(BuildTag(2, "Electronics"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetTags();
            // Assert
            Assert.Equal(2, result.data.Count());
            Assert.Equal(2, result.pagination.total);
        }

        [Fact]
        public async Task GetTags_ShouldFilterByIdResearch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Tags.Add(BuildTag(1, "A"));
            context.Tags.Add(BuildTag(2, "B"));
            context.Tags.Add(BuildTag(3, "C"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetTags(idResearch: new List<int> { 2 });
            // Assert
            var tag = Assert.Single(result.data);
            Assert.Equal(2, tag.id_tag);
        }

        // --- GetTagById ---

        [Fact]
        public async Task GetTagById_ShouldReturnTag_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Tags.Add(BuildTag(1, "Fragile"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetTagById(1);
            // Assert
            Assert.Equal("Fragile", result.nom_tag);
        }

        [Fact]
        public async Task GetTagById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetTagById(999);
            });
        }

        // --- CreateTag ---

        [Fact]
        public async Task CreateTag_ShouldCreateTag_WhenNameUnique()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateTagDto { nom_tag = "Fragile", poids_tag = 5 };
            // Act
            var result = await service.CreateTag(dto);
            // Assert
            Assert.Equal("Fragile", result.nom_tag);
            Assert.Equal(1, await context.Tags.CountAsync());
        }

        [Fact]
        public async Task CreateTag_ShouldThrowInvalidOperationException_WhenNameAlreadyExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Tags.Add(BuildTag(1, "Fragile"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateTagDto { nom_tag = "Fragile", poids_tag = 5 };
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.CreateTag(dto);
            });
        }

        // --- CreateBulkTag ---

        [Fact]
        public async Task CreateBulkTag_ShouldReturnMixedResults_WhenOneNameIsDuplicate()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Tags.Add(BuildTag(1, "Fragile"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dtos = new List<CreateTagDto>
            {
                new() { nom_tag = "Fragile", poids_tag = 1 },
                new() { nom_tag = "Electronics", poids_tag = 1 }
            };
            // Act
            var result = await service.CreateBulkTag(dtos);
            // Assert
            Assert.Single(result.Valide);
            Assert.Single(result.Error);
            Assert.Equal(2, await context.Tags.CountAsync());
        }

        // --- UpdateTag ---

        [Fact]
        public async Task UpdateTag_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateTag(999, new UpdateTagDto());
            });
        }

        [Fact]
        public async Task UpdateTag_ShouldThrowInvalidOperationException_WhenNameAlreadyUsedByAnotherTag()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Tags.Add(BuildTag(1, "Fragile"));
            context.Tags.Add(BuildTag(2, "Electronics"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.UpdateTag(2, new UpdateTagDto { nom_tag = "Fragile" });
            });
        }

        [Fact]
        public async Task UpdateTag_ShouldUpdateFields_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Tags.Add(BuildTag(1, "Old name"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.UpdateTag(1, new UpdateTagDto { nom_tag = "New name", poids_tag = 9 });
            // Assert
            Assert.Equal("New name", result.nom_tag);
            Assert.Equal(9, result.poids_tag);
        }

        // --- DeleteTag ---

        [Fact]
        public async Task DeleteTag_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeleteTag(999);
            });
        }

        [Fact]
        public async Task DeleteTag_ShouldDeleteTag_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Tags.Add(BuildTag(1, "Fragile"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            await service.DeleteTag(1);
            // Assert
            Assert.Equal(0, await context.Tags.CountAsync());
        }
    }
}
