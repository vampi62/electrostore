using Microsoft.EntityFrameworkCore;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.ProjetStatusService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class ProjetStatusServiceTests : TestBase
    {
        private ProjetStatusService CreateService(ApplicationDbContext context)
        {
            return new ProjetStatusService(_mapper, context);
        }

        private static Projets BuildProjet(int id, string name = "Projet")
        {
            return new Projets
            {
                id_projet = id,
                nom_projet = name
            };
        }

        private static ProjetsStatus BuildProjetStatus(int id, int projetId, ProjetStatus status = ProjetStatus.InProgress)
        {
            return new ProjetsStatus
            {
                id_projet_status = id,
                id_projet = projetId,
                status_projet = status
            };
        }

        // --- GetProjetStatusByProjetId ---

        [Fact]
        public async Task GetProjetStatusByProjetId_ShouldThrowKeyNotFoundException_WhenProjetNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetProjetStatusByProjetId(1);
            });
        }

        [Fact]
        public async Task GetProjetStatusByProjetId_ShouldReturnOnlyStatusesForGivenProjet()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Projets.Add(BuildProjet(2));
            context.ProjetsStatus.Add(BuildProjetStatus(1, 1));
            context.ProjetsStatus.Add(BuildProjetStatus(2, 1));
            context.ProjetsStatus.Add(BuildProjetStatus(3, 2));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetProjetStatusByProjetId(1);
            // Assert
            Assert.Equal(2, result.data.Count());
            Assert.All(result.data, s => Assert.Equal(1, s.id_projet));
            Assert.Equal(2, result.pagination.total);
        }

        // --- GetProjetStatusById ---

        [Fact]
        public async Task GetProjetStatusById_ShouldReturnStatus_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.ProjetsStatus.Add(BuildProjetStatus(1, 1, ProjetStatus.Completed));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetProjetStatusById(1);
            // Assert
            Assert.Equal(1, result.id_projet_status);
            Assert.Equal(ProjetStatus.Completed, result.status_projet);
        }

        [Fact]
        public async Task GetProjetStatusById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetProjetStatusById(999);
            });
        }

        [Fact]
        public async Task GetProjetStatusById_ShouldThrowKeyNotFoundException_WhenProjetIdDoesNotMatch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Projets.Add(BuildProjet(2));
            context.ProjetsStatus.Add(BuildProjetStatus(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetProjetStatusById(1, projetId: 2);
            });
        }

        // --- CreateProjetStatus ---

        [Fact]
        public async Task CreateProjetStatus_ShouldThrowKeyNotFoundException_WhenProjetNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateProjetStatusDto { id_projet = 999, status_projet = ProjetStatus.InProgress };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreateProjetStatus(dto);
            });
        }

        [Fact]
        public async Task CreateProjetStatus_ShouldCreateStatus_WhenProjetExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateProjetStatusDto { id_projet = 1, status_projet = ProjetStatus.Completed };
            // Act
            var result = await service.CreateProjetStatus(dto);
            // Assert
            Assert.Equal(1, result.id_projet);
            Assert.Equal(ProjetStatus.Completed, result.status_projet);
            Assert.Equal(1, await context.ProjetsStatus.CountAsync());
        }
    }
}
