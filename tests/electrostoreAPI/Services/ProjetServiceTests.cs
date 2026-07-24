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
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Services.ProjetService;
using ElectrostoreAPI.Services.ProjetStatusService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class ProjetServiceTests : TestBase
    {
        private readonly Mock<IFileService> _fileService;
        private readonly Mock<IProjetStatusService> _projetStatusService;

        public ProjetServiceTests()
        {
            _fileService = new Mock<IFileService>();
            _projetStatusService = new Mock<IProjetStatusService>();
        }

        private ProjetService CreateService(ApplicationDbContext context)
        {
            return new ProjetService(_mapper, context, _fileService.Object, _projetStatusService.Object);
        }

        private static Projets BuildProjet(int id, string name = "Projet", ProjetStatus status = ProjetStatus.NotStarted)
        {
            return new Projets
            {
                id_projet = id,
                nom_projet = name,
                status_projet = status
            };
        }

        // --- GetProjets ---

        [Fact]
        public async Task GetProjets_ShouldReturnAll_Paginated()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Projets.Add(BuildProjet(2));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetProjets();
            // Assert
            Assert.Equal(2, result.data.Count());
            Assert.Equal(2, result.pagination.total);
        }

        [Fact]
        public async Task GetProjets_ShouldFilterByIdResearch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Projets.Add(BuildProjet(2));
            context.Projets.Add(BuildProjet(3));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetProjets(idResearch: new List<int> { 2 });
            // Assert
            var projet = Assert.Single(result.data);
            Assert.Equal(2, projet.id_projet);
        }

        // --- GetProjetById ---

        [Fact]
        public async Task GetProjetById_ShouldReturnProjet_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1, "My Projet"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetProjetById(1);
            // Assert
            Assert.Equal("My Projet", result.nom_projet);
        }

        [Fact]
        public async Task GetProjetById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetProjetById(999);
            });
        }

        // --- CreateProjet ---

        [Fact]
        public async Task CreateProjet_ShouldCreateProjetDirectoryAndInitialStatus()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateProjetDto { nom_projet = "New Projet", status_projet = ProjetStatus.InProgress };
            // Act
            var result = await service.CreateProjet(dto);
            // Assert
            Assert.Equal("New Projet", result.nom_projet);
            Assert.Equal(1, await context.Projets.CountAsync());
            _fileService.Verify(f => f.CreateDirectory(It.IsAny<string>()), Times.Once);
            _projetStatusService.Verify(s => s.CreateProjetStatus(It.Is<CreateProjetStatusDto>(d => d.id_projet == result.id_projet && d.status_projet == ProjetStatus.InProgress)), Times.Once);
        }

        // --- UpdateProjet ---

        [Fact]
        public async Task UpdateProjet_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateProjet(999, new UpdateProjetDto());
            });
        }

        [Fact]
        public async Task UpdateProjet_ShouldUpdateFields_WithoutCreatingStatusEntry_WhenStatusUnchanged()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1, "Old name", ProjetStatus.NotStarted));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.UpdateProjet(1, new UpdateProjetDto { nom_projet = "New name", status_projet = ProjetStatus.NotStarted });
            // Assert
            Assert.Equal("New name", result.nom_projet);
            _projetStatusService.Verify(s => s.CreateProjetStatus(It.IsAny<CreateProjetStatusDto>()), Times.Never);
        }

        [Fact]
        public async Task UpdateProjet_ShouldCreateNewStatusEntry_WhenStatusChanged()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1, "Projet", ProjetStatus.NotStarted));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.UpdateProjet(1, new UpdateProjetDto { status_projet = ProjetStatus.Completed });
            // Assert
            Assert.Equal(ProjetStatus.Completed, result.status_projet);
            _projetStatusService.Verify(s => s.CreateProjetStatus(It.Is<CreateProjetStatusDto>(d => d.id_projet == 1 && d.status_projet == ProjetStatus.Completed)), Times.Once);
        }

        // --- DeleteProjet ---

        [Fact]
        public async Task DeleteProjet_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeleteProjet(999);
            });
        }

        [Fact]
        public async Task DeleteProjet_ShouldDeleteProjetAndDirectory()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            await service.DeleteProjet(1);
            // Assert
            Assert.Equal(0, await context.Projets.CountAsync());
            _fileService.Verify(f => f.DeleteDirectory(It.IsAny<string>()), Times.Once);
        }
    }
}
