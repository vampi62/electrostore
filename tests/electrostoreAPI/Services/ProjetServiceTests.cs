using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using electrostore;
using electrostore.Dto;
using electrostore.Enums;
using electrostore.Models;
using electrostore.Services.FileService;
using electrostore.Services.ProjetService;
using electrostore.Services.ProjetStatusService;
using electrostore.Tests.Utils;

namespace electrostore.Tests.Services
{
    public class ProjetServiceTests : TestBase
    {
        private readonly Mock<IFileService> _mockFileService;
        private readonly Mock<IProjetStatusService> _mockProjetStatusService;

        public ProjetServiceTests()
        {
            _mockFileService = new Mock<IFileService>();
            _mockFileService.Setup(fs => fs.CreateDirectory(It.IsAny<string>())).Returns(Task.CompletedTask);
            _mockFileService.Setup(fs => fs.DeleteDirectory(It.IsAny<string>())).Returns(Task.CompletedTask);

            _mockProjetStatusService = new Mock<IProjetStatusService>();
            _mockProjetStatusService
                .Setup(ps => ps.CreateProjetStatus(It.IsAny<CreateProjetStatusDto>()))
                .ReturnsAsync(new ReadProjetStatusDto
                {
                    id_projet_status = 1,
                    id_projet = 1,
                    status_projet = ProjetStatus.NotStarted,
                    created_at = DateTime.UtcNow,
                    updated_at = DateTime.UtcNow
                });
        }

        [Fact]
        public async Task GetProjets_ShouldReturnProjets_WhenProjetsExist()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);

            for (int i = 1; i <= 5; i++)
            {
                context.Projets.Add(new Projets
                {
                    id_projet = i,
                    nom_projet = $"Projet {i}",
                    description_projet = $"Description {i}",
                    url_projet = $"https://example.com/{i}",
                    status_projet = ProjetStatus.NotStarted
                });
            }
            await context.SaveChangesAsync();

            var service = new ProjetService(_mapper, context, _mockFileService.Object, _mockProjetStatusService.Object);

            // Act
            var result = await service.GetProjets(5, 0);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Count());
        }

        [Fact]
        public async Task GetProjetsCount_ShouldReturnCorrectCount()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);

            for (int i = 1; i <= 3; i++)
            {
                context.Projets.Add(new Projets
                {
                    id_projet = i,
                    nom_projet = $"Projet {i}",
                    description_projet = $"Description {i}",
                    url_projet = $"https://example.com/{i}",
                    status_projet = ProjetStatus.NotStarted
                });
            }
            await context.SaveChangesAsync();

            var service = new ProjetService(_mapper, context, _mockFileService.Object, _mockProjetStatusService.Object);

            // Act
            var count = await service.GetProjetsCount();

            // Assert
            Assert.Equal(3, count);
        }

        [Fact]
        public async Task GetProjetById_ShouldReturnProjet_WhenProjetExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Projets.Add(new Projets
            {
                id_projet = 1,
                nom_projet = "Projet 1",
                description_projet = "Description 1",
                url_projet = "https://example.com/1",
                status_projet = ProjetStatus.InProgress
            });
            await context.SaveChangesAsync();

            var service = new ProjetService(_mapper, context, _mockFileService.Object, _mockProjetStatusService.Object);

            var expected = new ReadExtendedProjetDto
            {
                id_projet = 1,
                nom_projet = "Projet 1",
                description_projet = "Description 1",
                url_projet = "https://example.com/1",
                status_projet = ProjetStatus.InProgress
            };

            // Act
            var result = await service.GetProjetById(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expected.id_projet, result.id_projet);
            Assert.Equal(expected.nom_projet, result.nom_projet);
            Assert.Equal(expected.description_projet, result.description_projet);
            Assert.Equal(expected.url_projet, result.url_projet);
            Assert.Equal(expected.status_projet, result.status_projet);
        }

        [Fact]
        public async Task CreateProjet_ShouldCreateProjet_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = new ProjetService(_mapper, context, _mockFileService.Object, _mockProjetStatusService.Object);

            var createDto = new CreateProjetDto
            {
                nom_projet = "Projet X",
                description_projet = "Description X",
                url_projet = "https://example.com/x",
                status_projet = ProjetStatus.NotStarted
            };

            // Act
            var result = await service.CreateProjet(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.id_projet);
            Assert.Equal("Projet X", result.nom_projet);
            Assert.Equal("Description X", result.description_projet);
            Assert.Equal("https://example.com/x", result.url_projet);
            Assert.Equal(ProjetStatus.NotStarted, result.status_projet);

            var projetInDb = await context.Projets.FirstOrDefaultAsync(p => p.nom_projet == "Projet X");
            Assert.NotNull(projetInDb);
        }

        [Fact]
        public async Task UpdateProjet_ShouldUpdateProjet_WhenProjetExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Projets.Add(new Projets
            {
                id_projet = 1,
                nom_projet = "Projet 1",
                description_projet = "Description 1",
                url_projet = "https://example.com/1",
                status_projet = ProjetStatus.NotStarted
            });
            await context.SaveChangesAsync();

            var service = new ProjetService(_mapper, context, _mockFileService.Object, _mockProjetStatusService.Object);

            var updateDto = new UpdateProjetDto
            {
                nom_projet = "Projet 1 Updated",
                description_projet = "Description 1 Updated",
                url_projet = "https://example.com/1-updated",
                status_projet = ProjetStatus.Completed
            };

            // Act
            var result = await service.UpdateProjet(1, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.id_projet);
            Assert.Equal("Projet 1 Updated", result.nom_projet);
            Assert.Equal("Description 1 Updated", result.description_projet);
            Assert.Equal("https://example.com/1-updated", result.url_projet);
            Assert.Equal(ProjetStatus.Completed, result.status_projet);

            var projetInDb = await context.Projets.FindAsync(1);
            Assert.NotNull(projetInDb);
            Assert.Equal("Projet 1 Updated", projetInDb.nom_projet);
            Assert.Equal("Description 1 Updated", projetInDb.description_projet);
            Assert.Equal("https://example.com/1-updated", projetInDb.url_projet);
            Assert.Equal(ProjetStatus.Completed, projetInDb.status_projet);
        }

        [Fact]
        public async Task DeleteProjet_ShouldDeleteProjet_WhenProjetExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Projets.Add(new Projets
            {
                id_projet = 1,
                nom_projet = "Projet 1",
                description_projet = "Description 1",
                url_projet = "https://example.com/1",
                status_projet = ProjetStatus.NotStarted
            });
            await context.SaveChangesAsync();

            var service = new ProjetService(_mapper, context, _mockFileService.Object, _mockProjetStatusService.Object);

            // Act
            await service.DeleteProjet(1);

            // Assert
            var projetInDb = await context.Projets.FindAsync(1);
            Assert.Null(projetInDb);
        }
    }
}