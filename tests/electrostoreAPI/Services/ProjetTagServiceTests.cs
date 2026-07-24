using Microsoft.EntityFrameworkCore;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.ProjetTagService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class ProjetTagServiceTests : TestBase
    {
        private ProjetTagService CreateService(ApplicationDbContext context)
        {
            return new ProjetTagService(_mapper, context);
        }

        private static ProjetTags BuildProjetTag(int id, string name, int weight = 0)
        {
            return new ProjetTags
            {
                id_projet_tag = id,
                nom_projet_tag = name,
                poids_projet_tag = weight
            };
        }

        // --- GetProjetTags ---

        [Fact]
        public async Task GetProjetTags_ShouldReturnAll_Paginated()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.ProjetTags.Add(BuildProjetTag(1, "Urgent"));
            context.ProjetTags.Add(BuildProjetTag(2, "Low priority"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetProjetTags();
            // Assert
            Assert.Equal(2, result.data.Count());
            Assert.Equal(2, result.pagination.total);
        }

        [Fact]
        public async Task GetProjetTags_ShouldFilterByIdResearch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.ProjetTags.Add(BuildProjetTag(1, "A"));
            context.ProjetTags.Add(BuildProjetTag(2, "B"));
            context.ProjetTags.Add(BuildProjetTag(3, "C"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetProjetTags(idResearch: new List<int> { 2 });
            // Assert
            var tag = Assert.Single(result.data);
            Assert.Equal(2, tag.id_projet_tag);
        }

        // --- GetProjetTagById ---

        [Fact]
        public async Task GetProjetTagById_ShouldReturnTag_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.ProjetTags.Add(BuildProjetTag(1, "Urgent"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetProjetTagById(1);
            // Assert
            Assert.Equal("Urgent", result.nom_projet_tag);
        }

        [Fact]
        public async Task GetProjetTagById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetProjetTagById(999);
            });
        }

        // --- CreateProjetTag ---

        [Fact]
        public async Task CreateProjetTag_ShouldCreateTag_WhenNameUnique()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateProjetTagDto { nom_projet_tag = "Urgent", poids_projet_tag = 5 };
            // Act
            var result = await service.CreateProjetTag(dto);
            // Assert
            Assert.Equal("Urgent", result.nom_projet_tag);
            Assert.Equal(1, await context.ProjetTags.CountAsync());
        }

        [Fact]
        public async Task CreateProjetTag_ShouldThrowInvalidOperationException_WhenNameAlreadyExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.ProjetTags.Add(BuildProjetTag(1, "Urgent"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateProjetTagDto { nom_projet_tag = "Urgent", poids_projet_tag = 5 };
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.CreateProjetTag(dto);
            });
        }

        // --- CreateBulkProjetTag ---

        [Fact]
        public async Task CreateBulkProjetTag_ShouldReturnMixedResults_WhenOneNameIsDuplicate()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.ProjetTags.Add(BuildProjetTag(1, "Urgent"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dtos = new List<CreateProjetTagDto>
            {
                new() { nom_projet_tag = "Urgent", poids_projet_tag = 1 },
                new() { nom_projet_tag = "Low priority", poids_projet_tag = 1 }
            };
            // Act
            var result = await service.CreateBulkProjetTag(dtos);
            // Assert
            Assert.Single(result.Valide);
            Assert.Single(result.Error);
            Assert.Equal(2, await context.ProjetTags.CountAsync());
        }

        // --- UpdateProjetTag ---

        [Fact]
        public async Task UpdateProjetTag_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateProjetTag(999, new UpdateProjetTagDto());
            });
        }

        [Fact]
        public async Task UpdateProjetTag_ShouldThrowInvalidOperationException_WhenNameAlreadyUsedByAnotherTag()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.ProjetTags.Add(BuildProjetTag(1, "Urgent"));
            context.ProjetTags.Add(BuildProjetTag(2, "Low priority"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.UpdateProjetTag(2, new UpdateProjetTagDto { nom_projet_tag = "Urgent" });
            });
        }

        [Fact]
        public async Task UpdateProjetTag_ShouldUpdateFields_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.ProjetTags.Add(BuildProjetTag(1, "Old name"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.UpdateProjetTag(1, new UpdateProjetTagDto { nom_projet_tag = "New name", poids_projet_tag = 9 });
            // Assert
            Assert.Equal("New name", result.nom_projet_tag);
            Assert.Equal(9, result.poids_projet_tag);
        }

        // --- DeleteProjetTag ---

        [Fact]
        public async Task DeleteProjetTag_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeleteProjetTag(999);
            });
        }

        [Fact]
        public async Task DeleteProjetTag_ShouldDeleteTag_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.ProjetTags.Add(BuildProjetTag(1, "Urgent"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            await service.DeleteProjetTag(1);
            // Assert
            Assert.Equal(0, await context.ProjetTags.CountAsync());
        }
    }
}
