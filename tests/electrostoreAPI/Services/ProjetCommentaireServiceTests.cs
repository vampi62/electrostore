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
using ElectrostoreAPI.Services.ProjetCommentaireService;
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class ProjetCommentaireServiceTests : TestBase
    {
        private readonly Mock<ISessionService> _sessionService;

        public ProjetCommentaireServiceTests()
        {
            _sessionService = new Mock<ISessionService>();
        }

        private ProjetCommentaireService CreateService(ApplicationDbContext context)
        {
            return new ProjetCommentaireService(_mapper, context, _sessionService.Object);
        }

        private void SetClient(int id, UserRole role)
        {
            _sessionService.Setup(s => s.GetClientId()).Returns(id);
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

        private static Users BuildUser(int id)
        {
            return new Users
            {
                id_user = id,
                nom_user = "Nom",
                prenom_user = "Prenom",
                email_user = "user" + id + "@test.com",
                mdp_user = "hashedpassword"
            };
        }

        private static ProjetsCommentaires BuildCommentaire(int id, int projetId, int? userId, string content = "Comment")
        {
            return new ProjetsCommentaires
            {
                id_projet_commentaire = id,
                id_projet = projetId,
                id_user = userId,
                contenu_projet_commentaire = content
            };
        }

        // --- GetProjetCommentairesByProjetId ---

        [Fact]
        public async Task GetProjetCommentairesByProjetId_ShouldThrowKeyNotFoundException_WhenProjetNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetProjetCommentairesByProjetId(1);
            });
        }

        [Fact]
        public async Task GetProjetCommentairesByProjetId_ShouldReturnOnlyForGivenProjet()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Projets.Add(BuildProjet(2));
            context.Users.Add(BuildUser(1));
            context.ProjetsCommentaires.Add(BuildCommentaire(1, 1, 1));
            context.ProjetsCommentaires.Add(BuildCommentaire(2, 1, 1));
            context.ProjetsCommentaires.Add(BuildCommentaire(3, 2, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetProjetCommentairesByProjetId(1);
            // Assert
            Assert.Equal(2, result.data.Count());
            Assert.Equal(2, result.pagination.total);
        }

        // --- GetProjetCommentairesByUserId ---

        [Fact]
        public async Task GetProjetCommentairesByUserId_ShouldThrowKeyNotFoundException_WhenUserNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetProjetCommentairesByUserId(1);
            });
        }

        [Fact]
        public async Task GetProjetCommentairesByUserId_ShouldReturnOnlyForGivenUser()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Users.Add(BuildUser(1));
            context.Users.Add(BuildUser(2));
            context.ProjetsCommentaires.Add(BuildCommentaire(1, 1, 1));
            context.ProjetsCommentaires.Add(BuildCommentaire(2, 1, 2));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetProjetCommentairesByUserId(1);
            // Assert
            var commentaire = Assert.Single(result.data);
            Assert.Equal(1, commentaire.id_user);
        }

        // --- GetProjetCommentairesById ---

        [Fact]
        public async Task GetProjetCommentairesById_ShouldReturnCommentaire_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Users.Add(BuildUser(1));
            context.ProjetsCommentaires.Add(BuildCommentaire(1, 1, 1, "Hello"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetProjetCommentairesById(1);
            // Assert
            Assert.Equal("Hello", result.contenu_projet_commentaire);
        }

        [Fact]
        public async Task GetProjetCommentairesById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetProjetCommentairesById(999);
            });
        }

        // --- CreateProjetCommentaire ---

        [Fact]
        public async Task CreateProjetCommentaire_ShouldThrowKeyNotFoundException_WhenProjetNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateProjetCommentaireDto { id_projet = 999, id_user = 1, contenu_projet_commentaire = "Hello" };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreateProjetCommentaire(dto);
            });
        }

        [Fact]
        public async Task CreateProjetCommentaire_ShouldThrowKeyNotFoundException_WhenUserNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateProjetCommentaireDto { id_projet = 1, id_user = 999, contenu_projet_commentaire = "Hello" };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreateProjetCommentaire(dto);
            });
        }

        [Fact]
        public async Task CreateProjetCommentaire_ShouldCreateCommentaire_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Users.Add(BuildUser(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateProjetCommentaireDto { id_projet = 1, id_user = 1, contenu_projet_commentaire = "Hello" };
            // Act
            var result = await service.CreateProjetCommentaire(dto);
            // Assert
            Assert.Equal("Hello", result.contenu_projet_commentaire);
            Assert.Equal(1, await context.ProjetsCommentaires.CountAsync());
        }

        // --- UpdateProjetCommentaire ---

        [Fact]
        public async Task UpdateProjetCommentaire_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateProjetCommentaire(999, new UpdateProjetCommentaireDto());
            });
        }

        [Fact]
        public async Task UpdateProjetCommentaire_ShouldThrowUnauthorizedAccessException_WhenNotOwnerNorModerator()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Users.Add(BuildUser(1));
            context.ProjetsCommentaires.Add(BuildCommentaire(1, 1, 1));
            await context.SaveChangesAsync();
            SetClient(2, UserRole.User);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.UpdateProjetCommentaire(1, new UpdateProjetCommentaireDto { contenu_projet_commentaire = "Edited" });
            });
        }

        [Fact]
        public async Task UpdateProjetCommentaire_ShouldUpdateContent_WhenOwner()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Users.Add(BuildUser(1));
            context.ProjetsCommentaires.Add(BuildCommentaire(1, 1, 1, "Old"));
            await context.SaveChangesAsync();
            SetClient(1, UserRole.User);
            var service = CreateService(context);
            // Act
            var result = await service.UpdateProjetCommentaire(1, new UpdateProjetCommentaireDto { contenu_projet_commentaire = "New" });
            // Assert
            Assert.Equal("New", result.contenu_projet_commentaire);
        }

        [Fact]
        public async Task UpdateProjetCommentaire_ShouldUpdateContent_WhenModerator()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Users.Add(BuildUser(1));
            context.ProjetsCommentaires.Add(BuildCommentaire(1, 1, 1, "Old"));
            await context.SaveChangesAsync();
            SetClient(2, UserRole.Moderator);
            var service = CreateService(context);
            // Act
            var result = await service.UpdateProjetCommentaire(1, new UpdateProjetCommentaireDto { contenu_projet_commentaire = "New" });
            // Assert
            Assert.Equal("New", result.contenu_projet_commentaire);
        }

        // --- DeleteProjetCommentaire ---

        [Fact]
        public async Task DeleteProjetCommentaire_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeleteProjetCommentaire(999);
            });
        }

        [Fact]
        public async Task DeleteProjetCommentaire_ShouldThrowUnauthorizedAccessException_WhenNotOwnerNorModerator()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Users.Add(BuildUser(1));
            context.ProjetsCommentaires.Add(BuildCommentaire(1, 1, 1));
            await context.SaveChangesAsync();
            SetClient(2, UserRole.User);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.DeleteProjetCommentaire(1);
            });
        }

        [Fact]
        public async Task DeleteProjetCommentaire_ShouldDelete_WhenOwner()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Users.Add(BuildUser(1));
            context.ProjetsCommentaires.Add(BuildCommentaire(1, 1, 1));
            await context.SaveChangesAsync();
            SetClient(1, UserRole.User);
            var service = CreateService(context);
            // Act
            await service.DeleteProjetCommentaire(1);
            // Assert
            Assert.Equal(0, await context.ProjetsCommentaires.CountAsync());
        }
    }
}
