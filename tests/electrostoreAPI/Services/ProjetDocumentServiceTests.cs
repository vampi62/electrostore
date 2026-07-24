using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Services.ProjetDocumentService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class ProjetDocumentServiceTests : TestBase
    {
        private readonly Mock<IFileService> _fileService;

        public ProjetDocumentServiceTests()
        {
            _fileService = new Mock<IFileService>();
        }

        private ProjetDocumentService CreateService(ApplicationDbContext context)
        {
            return new ProjetDocumentService(_mapper, context, _fileService.Object);
        }

        private static Projets BuildProjet(int id, string name = "Projet")
        {
            return new Projets
            {
                id_projet = id,
                nom_projet = name
            };
        }

        private static ProjetsDocuments BuildProjetDocument(int id, int projetId, string name = "Document")
        {
            return new ProjetsDocuments
            {
                id_projet_document = id,
                id_projet = projetId,
                url_projet_document = "projetDocuments/" + projetId + "/" + id + ".pdf",
                name_projet_document = name,
                type_projet_document = "application/pdf",
                size_projet_document = 1024
            };
        }

        private static Mock<IFormFile> BuildFormFile(string fileName = "doc.pdf", string contentType = "application/pdf", long length = 1024)
        {
            var file = new Mock<IFormFile>();
            file.Setup(f => f.FileName).Returns(fileName);
            file.Setup(f => f.ContentType).Returns(contentType);
            file.Setup(f => f.Length).Returns(length);
            file.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
            return file;
        }

        // --- GetProjetDocumentsByProjetId ---

        [Fact]
        public async Task GetProjetDocumentsByProjetId_ShouldThrowKeyNotFoundException_WhenProjetNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetProjetDocumentsByProjetId(1);
            });
        }

        [Fact]
        public async Task GetProjetDocumentsByProjetId_ShouldReturnOnlyDocumentsForGivenProjet()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Projets.Add(BuildProjet(2));
            context.ProjetsDocuments.Add(BuildProjetDocument(1, 1));
            context.ProjetsDocuments.Add(BuildProjetDocument(2, 1));
            context.ProjetsDocuments.Add(BuildProjetDocument(3, 2));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetProjetDocumentsByProjetId(1);
            // Assert
            Assert.Equal(2, result.data.Count());
            Assert.All(result.data, d => Assert.Equal(1, d.id_projet));
            Assert.Equal(2, result.pagination.total);
        }

        // --- GetProjetDocumentById ---

        [Fact]
        public async Task GetProjetDocumentById_ShouldReturnDocument_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.ProjetsDocuments.Add(BuildProjetDocument(1, 1, "Specs"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetProjetDocumentById(1);
            // Assert
            Assert.Equal("Specs", result.name_projet_document);
        }

        [Fact]
        public async Task GetProjetDocumentById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetProjetDocumentById(999);
            });
        }

        [Fact]
        public async Task GetProjetDocumentById_ShouldThrowKeyNotFoundException_WhenProjetIdDoesNotMatch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Projets.Add(BuildProjet(2));
            context.ProjetsDocuments.Add(BuildProjetDocument(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetProjetDocumentById(1, projetId: 2);
            });
        }

        // --- CreateProjetDocument ---

        [Fact]
        public async Task CreateProjetDocument_ShouldThrowKeyNotFoundException_WhenProjetNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateProjetDocumentDto { id_projet = 999, name_projet_document = "Specs", document = BuildFormFile().Object };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreateProjetDocument(dto);
            });
        }

        [Fact]
        public async Task CreateProjetDocument_ShouldCreateDocument_WhenProjetExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            await context.SaveChangesAsync();
            _fileService.Setup(f => f.SaveFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<bool>()))
                .ReturnsAsync(new SaveFileResult { path = "projetDocuments/1/doc.pdf", mimeType = "application/pdf" });
            var service = CreateService(context);
            var dto = new CreateProjetDocumentDto { id_projet = 1, name_projet_document = "Specs", document = BuildFormFile().Object };
            // Act
            var result = await service.CreateProjetDocument(dto);
            // Assert
            Assert.Equal(1, result.id_projet);
            Assert.Equal("Specs", result.name_projet_document);
            Assert.Equal("projetDocuments/1/doc.pdf", result.url_projet_document);
            Assert.Equal(1, await context.ProjetsDocuments.CountAsync());
        }

        // --- UpdateProjetDocument ---

        [Fact]
        public async Task UpdateProjetDocument_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateProjetDocument(999, new UpdateProjetDocumentDto());
            });
        }

        [Fact]
        public async Task UpdateProjetDocument_ShouldThrowKeyNotFoundException_WhenProjetIdDoesNotMatch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Projets.Add(BuildProjet(2));
            context.ProjetsDocuments.Add(BuildProjetDocument(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateProjetDocument(1, new UpdateProjetDocumentDto(), projetId: 2);
            });
        }

        [Fact]
        public async Task UpdateProjetDocument_ShouldUpdateName_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.ProjetsDocuments.Add(BuildProjetDocument(1, 1, "Old name"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.UpdateProjetDocument(1, new UpdateProjetDocumentDto { name_projet_document = "New name" });
            // Assert
            Assert.Equal("New name", result.name_projet_document);
        }

        // --- DeleteProjetDocument ---

        [Fact]
        public async Task DeleteProjetDocument_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeleteProjetDocument(999);
            });
        }

        [Fact]
        public async Task DeleteProjetDocument_ShouldThrowKeyNotFoundException_WhenProjetIdDoesNotMatch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Projets.Add(BuildProjet(2));
            context.ProjetsDocuments.Add(BuildProjetDocument(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeleteProjetDocument(1, projetId: 2);
            });
        }

        [Fact]
        public async Task DeleteProjetDocument_ShouldDeleteDocumentAndFile_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.ProjetsDocuments.Add(BuildProjetDocument(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            await service.DeleteProjetDocument(1);
            // Assert
            Assert.Equal(0, await context.ProjetsDocuments.CountAsync());
            _fileService.Verify(f => f.DeleteFile(It.IsAny<string>()), Times.Once);
        }
    }
}
