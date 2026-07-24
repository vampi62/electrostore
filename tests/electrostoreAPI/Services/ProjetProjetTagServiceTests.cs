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
using ElectrostoreAPI.Services.ProjetProjetTagService;
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class ProjetProjetTagServiceTests : TestBase
    {
        private readonly Mock<ISessionService> _sessionService;

        public ProjetProjetTagServiceTests()
        {
            _sessionService = new Mock<ISessionService>();
        }

        private ProjetProjetTagService CreateService(ApplicationDbContext context)
        {
            return new ProjetProjetTagService(_mapper, context, _sessionService.Object);
        }

        private void SetClientRole(UserRole role)
        {
            _sessionService.Setup(s => s.GetClientRole()).Returns(role);
        }

        private static Projets BuildProjet(int id, string name = "Projet")
        {
            return new Projets
            {
                id_projet = id,
                nom_projet = name
            };
        }

        private static ProjetTags BuildProjetTag(int id, string name = "ProjetTag")
        {
            return new ProjetTags
            {
                id_projet_tag = id,
                nom_projet_tag = name
            };
        }

        private static ProjetsProjetTags BuildProjetProjetTag(int projetId, int projetTagId)
        {
            return new ProjetsProjetTags
            {
                id_projet = projetId,
                id_projet_tag = projetTagId
            };
        }

        // --- GetProjetsProjetTagsByProjetId ---

        [Fact]
        public async Task GetProjetsProjetTagsByProjetId_ShouldThrowKeyNotFoundException_WhenProjetNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetProjetsProjetTagsByProjetId(1);
            });
        }

        [Fact]
        public async Task GetProjetsProjetTagsByProjetId_ShouldReturnOnlyTagsForGivenProjet()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Projets.Add(BuildProjet(2));
            context.ProjetTags.Add(BuildProjetTag(1));
            context.ProjetTags.Add(BuildProjetTag(2));
            context.ProjetsProjetTags.Add(BuildProjetProjetTag(1, 1));
            context.ProjetsProjetTags.Add(BuildProjetProjetTag(1, 2));
            context.ProjetsProjetTags.Add(BuildProjetProjetTag(2, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetProjetsProjetTagsByProjetId(1);
            // Assert
            Assert.Equal(2, result.data.Count());
        }

        // --- GetProjetsProjetTagsByprojetTagId ---

        [Fact]
        public async Task GetProjetsProjetTagsByprojetTagId_ShouldThrowKeyNotFoundException_WhenTagNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetProjetsProjetTagsByprojetTagId(1);
            });
        }

        [Fact]
        public async Task GetProjetsProjetTagsByprojetTagId_ShouldReturnOnlyProjetsForGivenTag()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Projets.Add(BuildProjet(2));
            context.ProjetTags.Add(BuildProjetTag(1));
            context.ProjetsProjetTags.Add(BuildProjetProjetTag(1, 1));
            context.ProjetsProjetTags.Add(BuildProjetProjetTag(2, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetProjetsProjetTagsByprojetTagId(1);
            // Assert
            Assert.Equal(2, result.data.Count());
        }

        // --- GetProjetProjetTagById ---

        [Fact]
        public async Task GetProjetProjetTagById_ShouldReturnEntry_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.ProjetTags.Add(BuildProjetTag(1));
            context.ProjetsProjetTags.Add(BuildProjetProjetTag(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetProjetProjetTagById(1, 1);
            // Assert
            Assert.Equal(1, result.id_projet);
            Assert.Equal(1, result.id_projet_tag);
        }

        [Fact]
        public async Task GetProjetProjetTagById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetProjetProjetTagById(999, 999);
            });
        }

        // --- CreateProjetProjetTag ---

        [Fact]
        public async Task CreateProjetProjetTag_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.User);
            var service = CreateService(context);
            var dto = new CreateProjetProjetTagDto { id_projet = 1, id_projet_tag = 1 };
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.CreateProjetProjetTag(dto);
            });
        }

        [Fact]
        public async Task CreateProjetProjetTag_ShouldThrowKeyNotFoundException_WhenProjetNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.ProjetTags.Add(BuildProjetTag(1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            var dto = new CreateProjetProjetTagDto { id_projet = 999, id_projet_tag = 1 };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreateProjetProjetTag(dto);
            });
        }

        [Fact]
        public async Task CreateProjetProjetTag_ShouldThrowKeyNotFoundException_WhenTagNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            var dto = new CreateProjetProjetTagDto { id_projet = 1, id_projet_tag = 999 };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreateProjetProjetTag(dto);
            });
        }

        [Fact]
        public async Task CreateProjetProjetTag_ShouldThrowInvalidOperationException_WhenAlreadyExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.ProjetTags.Add(BuildProjetTag(1));
            context.ProjetsProjetTags.Add(BuildProjetProjetTag(1, 1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            var dto = new CreateProjetProjetTagDto { id_projet = 1, id_projet_tag = 1 };
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.CreateProjetProjetTag(dto);
            });
        }

        [Fact]
        public async Task CreateProjetProjetTag_ShouldCreateEntry_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.ProjetTags.Add(BuildProjetTag(1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            var dto = new CreateProjetProjetTagDto { id_projet = 1, id_projet_tag = 1 };
            // Act
            var result = await service.CreateProjetProjetTag(dto);
            // Assert
            Assert.Equal(1, result.id_projet);
            Assert.Equal(1, await context.ProjetsProjetTags.CountAsync());
        }

        // --- CreateBulkProjetProjetTag ---

        [Fact]
        public async Task CreateBulkProjetProjetTag_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.User);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.CreateBulkProjetProjetTag(new List<CreateProjetProjetTagDto>());
            });
        }

        [Fact]
        public async Task CreateBulkProjetProjetTag_ShouldReturnMixedResults_WhenOneProjetMissing()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.ProjetTags.Add(BuildProjetTag(1));
            context.ProjetTags.Add(BuildProjetTag(2));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            var dtos = new List<CreateProjetProjetTagDto>
            {
                new() { id_projet = 1, id_projet_tag = 1 },
                new() { id_projet = 999, id_projet_tag = 2 }
            };
            // Act
            var result = await service.CreateBulkProjetProjetTag(dtos);
            // Assert
            Assert.Single(result.Valide);
            Assert.Single(result.Error);
        }

        // --- DeleteProjetProjetTag ---

        [Fact]
        public async Task DeleteProjetProjetTag_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.User);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.DeleteProjetProjetTag(1, 1);
            });
        }

        [Fact]
        public async Task DeleteProjetProjetTag_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeleteProjetProjetTag(999, 999);
            });
        }

        [Fact]
        public async Task DeleteProjetProjetTag_ShouldDeleteEntry_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.ProjetTags.Add(BuildProjetTag(1));
            context.ProjetsProjetTags.Add(BuildProjetProjetTag(1, 1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            // Act
            await service.DeleteProjetProjetTag(1, 1);
            // Assert
            Assert.Equal(0, await context.ProjetsProjetTags.CountAsync());
        }

        // --- DeleteBulkProjetProjetTag ---

        [Fact]
        public async Task DeleteBulkProjetProjetTag_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.User);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.DeleteBulkProjetProjetTag(new List<CreateProjetProjetTagDto>());
            });
        }

        [Fact]
        public async Task DeleteBulkProjetProjetTag_ShouldReturnMixedResults_WhenOneMissing()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.ProjetTags.Add(BuildProjetTag(1));
            context.ProjetsProjetTags.Add(BuildProjetProjetTag(1, 1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            var dtos = new List<CreateProjetProjetTagDto>
            {
                new() { id_projet = 1, id_projet_tag = 1 },
                new() { id_projet = 999, id_projet_tag = 999 }
            };
            // Act
            var result = await service.DeleteBulkProjetProjetTag(dtos);
            // Assert
            Assert.Single(result.Valide);
            Assert.Single(result.Error);
        }
    }
}
