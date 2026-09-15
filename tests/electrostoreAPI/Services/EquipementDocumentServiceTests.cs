using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.EquipementDocumentService;
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class EquipementDocumentServiceTests : TestBase
    {
        private readonly Mock<IFileService> _fileService = new();

        private EquipementDocumentService CreateService(ApplicationDbContext context) =>
            new(_mapper, context, _fileService.Object);

        private static Equipements BuildEquipement(string reference = "eq") => new()
        {
            reference_name_equipement = reference,
            friendly_name_equipement = reference
        };

        private static Mock<IFormFile> BuildFormFile(string fileName = "doc.pdf", string contentType = "application/pdf", long length = 1024)
        {
            var file = new Mock<IFormFile>();
            file.Setup(f => f.FileName).Returns(fileName);
            file.Setup(f => f.ContentType).Returns(contentType);
            file.Setup(f => f.Length).Returns(length);
            return file;
        }

        // --- GetEquipementsDocumentsByEquipementId ---

        [Fact]
        public async Task GetEquipementsDocumentsByEquipementId_ShouldReturnDocumentsForEquipement()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            context.EquipementsDocuments.Add(new EquipementsDocuments
            {
                id_equipement = equipement.id_equipement,
                url_equipement_document = "path/doc.pdf",
                name_equipement_document = "doc",
                type_equipement_document = "application/pdf",
                size_equipement_document = 100
            });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetEquipementsDocumentsByEquipementId(equipement.id_equipement);

            Assert.Single(result.data);
        }

        [Fact]
        public async Task GetEquipementsDocumentsByEquipementId_ShouldThrowKeyNotFoundException_WhenEquipementDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetEquipementsDocumentsByEquipementId(999));
        }

        // --- GetEquipementDocumentById ---

        [Fact]
        public async Task GetEquipementDocumentById_ShouldReturnDocument_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            var document = new EquipementsDocuments
            {
                id_equipement = equipement.id_equipement,
                url_equipement_document = "path/doc.pdf",
                name_equipement_document = "doc",
                type_equipement_document = "application/pdf",
                size_equipement_document = 100
            };
            context.EquipementsDocuments.Add(document);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetEquipementDocumentById(document.id_equipement_document);

            Assert.Equal(document.id_equipement_document, result.id_equipement_document);
        }

        [Fact]
        public async Task GetEquipementDocumentById_ShouldThrowKeyNotFoundException_WhenEquipementIdDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            var document = new EquipementsDocuments
            {
                id_equipement = equipement.id_equipement,
                url_equipement_document = "path/doc.pdf",
                name_equipement_document = "doc",
                type_equipement_document = "application/pdf",
                size_equipement_document = 100
            };
            context.EquipementsDocuments.Add(document);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetEquipementDocumentById(document.id_equipement_document, equipement.id_equipement + 1));
        }

        [Fact]
        public async Task GetEquipementDocumentById_ShouldThrowKeyNotFoundException_WhenDocumentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetEquipementDocumentById(999));
        }

        // --- CreateEquipementDocument ---

        [Fact]
        public async Task CreateEquipementDocument_ShouldPersistDocument_WhenEquipementExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            _fileService.Setup(f => f.SaveFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<bool>()))
                .ReturnsAsync(new SaveFileResult { path = "equipementDocuments/1/doc.pdf", mime_type = "application/pdf" });
            var service = CreateService(context);
            var dto = new CreateEquipementDocumentDto { id_equipement = equipement.id_equipement, name_equipement_document = "doc", document = BuildFormFile().Object };

            var result = await service.CreateEquipementDocument(dto);

            Assert.Equal("doc", result.name_equipement_document);
            Assert.Equal("equipementDocuments/1/doc.pdf", result.url_equipement_document);
            Assert.Equal(1, await context.EquipementsDocuments.CountAsync());
        }

        [Fact]
        public async Task CreateEquipementDocument_ShouldThrowKeyNotFoundException_WhenEquipementDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateEquipementDocumentDto { id_equipement = 999, name_equipement_document = "doc", document = BuildFormFile().Object };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateEquipementDocument(dto));
        }

        // --- UpdateEquipementDocument ---

        [Fact]
        public async Task UpdateEquipementDocument_ShouldUpdateName()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            var document = new EquipementsDocuments
            {
                id_equipement = equipement.id_equipement,
                url_equipement_document = "path/doc.pdf",
                name_equipement_document = "doc",
                type_equipement_document = "application/pdf",
                size_equipement_document = 100
            };
            context.EquipementsDocuments.Add(document);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new UpdateEquipementDocumentDto { name_equipement_document = "renamed" };

            var result = await service.UpdateEquipementDocument(document.id_equipement_document, dto);

            Assert.Equal("renamed", result.name_equipement_document);
        }

        [Fact]
        public async Task UpdateEquipementDocument_ShouldThrowKeyNotFoundException_WhenDocumentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateEquipementDocument(999, new UpdateEquipementDocumentDto()));
        }

        // --- DeleteEquipementDocument ---

        [Fact]
        public async Task DeleteEquipementDocument_ShouldRemoveDocument_AndDeleteFile()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            var document = new EquipementsDocuments
            {
                id_equipement = equipement.id_equipement,
                url_equipement_document = "path/doc.pdf",
                name_equipement_document = "doc",
                type_equipement_document = "application/pdf",
                size_equipement_document = 100
            };
            context.EquipementsDocuments.Add(document);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.DeleteEquipementDocument(document.id_equipement_document);

            Assert.Equal(0, await context.EquipementsDocuments.CountAsync());
            _fileService.Verify(f => f.DeleteFile("path/doc.pdf"), Times.Once);
        }

        [Fact]
        public async Task DeleteEquipementDocument_ShouldThrowKeyNotFoundException_WhenDocumentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteEquipementDocument(999));
        }
    }
}
