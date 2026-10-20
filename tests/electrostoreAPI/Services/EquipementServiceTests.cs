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
using ElectrostoreAPI.Services.EquipementService;
using ElectrostoreAPI.Services.EquipementStatusService;
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class EquipementServiceTests : TestBase
    {
        private readonly Mock<IFileService> _fileService = new();
        private readonly Mock<IEquipementStatusService> _equipementStatusService = new();

        private EquipementService CreateService(ApplicationDbContext context) =>
            new(_mapper, context, _fileService.Object, _equipementStatusService.Object);

        private static Equipements BuildEquipement(string reference = "eq") => new()
        {
            reference_name_equipement = reference,
            friendly_name_equipement = reference
        };

        // --- GetEquipements ---

        [Fact]
        public async Task GetEquipements_ShouldReturnAllEquipements_WhenNoFilterApplied()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Equipements.AddRange(BuildEquipement("a"), BuildEquipement("b"));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetEquipements();

            Assert.Equal(2, result.pagination.total);
            Assert.Equal(2, result.data.Count());
        }

        // --- GetEquipementById ---

        [Fact]
        public async Task GetEquipementById_ShouldReturnEquipement_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetEquipementById(equipement.id_equipement);

            Assert.Equal(equipement.id_equipement, result.id_equipement);
            Assert.Equal(0, result.equipement_tags_count);
        }

        [Fact]
        public async Task GetEquipementById_ShouldThrowKeyNotFoundException_WhenEquipementDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetEquipementById(999));
        }

        // --- CreateEquipement ---

        [Fact]
        public async Task CreateEquipement_ShouldPersistEquipement_AndCreateStatusHistoryEntry()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateEquipementDto
            {
                reference_name_equipement = "new-eq",
                friendly_name_equipement = "New equipement",
                status_equipement = EquipementStatus.Operational
            };

            var result = await service.CreateEquipement(dto);

            Assert.Equal("new-eq", result.reference_name_equipement);
            Assert.Equal(1, await context.Equipements.CountAsync());
            _fileService.Verify(f => f.CreateDirectory(It.IsAny<string>()), Times.Exactly(3));
            _equipementStatusService.Verify(s => s.CreateEquipementStatus(It.Is<CreateEquipementStatusDto>(
                d => d.id_equipement == result.id_equipement && d.status_equipement == EquipementStatus.Operational)), Times.Once);
        }

        [Fact]
        public async Task CreateEquipement_ShouldThrowInvalidOperationException_WhenReferenceNameAlreadyExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Equipements.Add(BuildEquipement("dup"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateEquipementDto
            {
                reference_name_equipement = "dup",
                friendly_name_equipement = "Dup",
                status_equipement = EquipementStatus.Operational
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateEquipement(dto));
        }

        // --- UpdateEquipement ---

        [Fact]
        public async Task UpdateEquipement_ShouldUpdateProvidedFields()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new UpdateEquipementDto { friendly_name_equipement = "renamed" };

            var result = await service.UpdateEquipement(equipement.id_equipement, dto);

            Assert.Equal("renamed", result.friendly_name_equipement);
            _equipementStatusService.Verify(s => s.CreateEquipementStatus(It.IsAny<CreateEquipementStatusDto>()), Times.Never);
        }

        [Fact]
        public async Task UpdateEquipement_ShouldCreateStatusHistoryEntry_WhenStatusChanges()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new UpdateEquipementDto { status_equipement = EquipementStatus.Retired };

            var result = await service.UpdateEquipement(equipement.id_equipement, dto);

            Assert.Equal(EquipementStatus.Retired, result.status_equipement);
            _equipementStatusService.Verify(s => s.CreateEquipementStatus(It.Is<CreateEquipementStatusDto>(
                d => d.id_equipement == equipement.id_equipement && d.status_equipement == EquipementStatus.Retired)), Times.Once);
        }

        [Fact]
        public async Task UpdateEquipement_ShouldThrowInvalidOperationException_WhenNewReferenceNameAlreadyUsedByAnotherEquipement()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement1 = BuildEquipement("a");
            var equipement2 = BuildEquipement("b");
            context.Equipements.AddRange(equipement1, equipement2);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new UpdateEquipementDto { reference_name_equipement = "b" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateEquipement(equipement1.id_equipement, dto));
        }

        [Fact]
        public async Task UpdateEquipement_ShouldThrowKeyNotFoundException_WhenEquipementDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateEquipement(999, new UpdateEquipementDto()));
        }

        // --- DeleteEquipement ---

        [Fact]
        public async Task DeleteEquipement_ShouldRemoveEquipement_AndDeleteDirectory()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.DeleteEquipement(equipement.id_equipement);

            Assert.Equal(0, await context.Equipements.CountAsync());
            _fileService.Verify(f => f.DeleteDirectory(It.IsAny<string>()), Times.Exactly(3));
        }

        [Fact]
        public async Task DeleteEquipement_ShouldThrowKeyNotFoundException_WhenEquipementDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteEquipement(999));
        }

        // --- additional coverage: expand, images, status ---

        private static Moq.Mock<Microsoft.AspNetCore.Http.IFormFile> BuildImageFile()
        {
            var file = new Moq.Mock<Microsoft.AspNetCore.Http.IFormFile>();
            file.Setup(f => f.FileName).Returns("pic.png");
            file.Setup(f => f.ContentType).Returns("image/png");
            file.Setup(f => f.OpenReadStream()).Returns(() => new System.IO.MemoryStream([1, 2, 3]));
            return file;
        }

        private void SetupImageSaving()
        {
            _fileService
                .Setup(f => f.SaveFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.IO.Stream>(), It.IsAny<bool>()))
                .ReturnsAsync(new SaveFileResult { path = "equipementImages/1/pic.png", mime_type = "image/png" });
            _fileService
                .Setup(f => f.GenerateThumbnail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new SaveFileResult { path = "equipementImagesThumbnails/1/pic.png", mime_type = "image/png" });
        }

        private static readonly List<string> AllExpands =
            ["equipement_tags", "equipement_boxs", "equipement_documents", "equipement_maintenances", "equipement_status_history", "equipement_comments"];

        private async Task<Equipements> SeedEquipementWithRelations(ApplicationDbContext context)
        {
            var equipement = BuildEquipement("full");
            var tag = new Tags { name_tag = "tag" };
            var store = new Stores { name_store = "s", mqtt_name_store = "s-mqtt", xlength_store = 10, ylength_store = 10 };
            context.Equipements.Add(equipement);
            context.Tags.Add(tag);
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = new Boxs { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 1, yend_box = 1 };
            context.Boxs.Add(box);
            await context.SaveChangesAsync();
            context.EquipementsTags.Add(new EquipementsTags { id_equipement = equipement.id_equipement, id_tag = tag.id_tag });
            context.EquipementsBoxs.Add(new EquipementsBoxs { id_equipement = equipement.id_equipement, id_box = box.id_box });
            context.EquipementsDocuments.Add(new EquipementsDocuments
            {
                id_equipement = equipement.id_equipement,
                url_equipement_document = "e/doc.pdf",
                name_equipement_document = "doc",
                type_equipement_document = "application/pdf",
                size_equipement_document = 1
            });
            context.EquipementsMaintenances.Add(new EquipementsMaintenances
            {
                id_equipement = equipement.id_equipement,
                type_equipement_maintenance = EquipementMaintenanceType.Preventive,
                date_planned_equipement_maintenance = DateTime.UtcNow
            });
            context.EquipementsStatus.Add(new EquipementsStatus { id_equipement = equipement.id_equipement, status_equipement = EquipementStatus.Operational });
            context.EquipementsComments.Add(new EquipementsComments { id_equipement = equipement.id_equipement, content_equipement_comment = "hi" });
            await context.SaveChangesAsync();
            return equipement;
        }

        [Fact]
        public async Task GetEquipements_ShouldRestrictToIds_WhenAnyFilterProvided()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var a = BuildEquipement("a");
            var b = BuildEquipement("b");
            context.Equipements.AddRange(a, b);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetEquipements(rsql: [new FilterDto { field = "id_equipement", search_type = "any", value = b.id_equipement.ToString() }]);

            var single = Assert.Single(result.data);
            Assert.Equal(b.id_equipement, single.id_equipement);
        }

        [Fact]
        public async Task GetEquipements_ShouldReturnCountsAndExpandedRelations()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            await SeedEquipementWithRelations(context);
            using var queryContext = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(queryContext);

            var result = await service.GetEquipements(expand: AllExpands);

            var single = Assert.Single(result.data);
            Assert.Equal(1, single.equipement_tags_count);
            Assert.Equal(1, single.equipement_boxs_count);
            Assert.Equal(1, single.equipement_documents_count);
            Assert.Equal(1, single.equipement_maintenances_count);
            Assert.Equal(1, single.equipement_status_history_count);
            Assert.Equal(1, single.equipement_comments_count);
            Assert.Single(single.equipement_tags!);
            Assert.Single(single.equipement_boxs!);
            Assert.Single(single.equipement_documents!);
            Assert.Single(single.equipement_maintenances!);
            Assert.Single(single.equipement_status_history!);
            Assert.Single(single.equipement_comments!);
        }

        [Fact]
        public async Task GetEquipementById_ShouldReturnExpandedRelations()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = await SeedEquipementWithRelations(context);
            using var queryContext = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(queryContext);

            var result = await service.GetEquipementById(equipement.id_equipement, AllExpands);

            Assert.Single(result.equipement_tags!);
            Assert.Single(result.equipement_boxs!);
            Assert.Single(result.equipement_documents!);
            Assert.Single(result.equipement_maintenances!);
            Assert.Single(result.equipement_status_history!);
            Assert.Single(result.equipement_comments!);
        }

        [Fact]
        public async Task CreateEquipement_ShouldSaveImageAndThumbnail_WhenImageProvided()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetupImageSaving();
            var service = CreateService(context);

            var result = await service.CreateEquipement(new CreateEquipementDto
            {
                reference_name_equipement = "img",
                friendly_name_equipement = "img",
                status_equipement = EquipementStatus.Operational,
                img_file = BuildImageFile().Object
            });

            Assert.Equal("equipementImages/1/pic.png", result.url_picture_equipement);
            Assert.Equal("equipementImagesThumbnails/1/pic.png", result.url_thumbnail_equipement);
        }

        [Fact]
        public async Task UpdateEquipement_ShouldUpdateFields_AndRecordStatusChange()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement("a");
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateEquipement(equipement.id_equipement, new UpdateEquipementDto
            {
                reference_name_equipement = "b",
                friendly_name_equipement = "friendly",
                description_equipement = "desc",
                status_equipement = EquipementStatus.InMaintenance
            });

            Assert.Equal("b", result.reference_name_equipement);
            Assert.Equal("friendly", result.friendly_name_equipement);
            Assert.Equal("desc", result.description_equipement);
            Assert.Equal(EquipementStatus.InMaintenance, result.status_equipement);
            _equipementStatusService.Verify(s => s.CreateEquipementStatus(It.Is<CreateEquipementStatusDto>(d =>
                d.id_equipement == equipement.id_equipement && d.status_equipement == EquipementStatus.InMaintenance)), Times.Once);
        }

        [Fact]
        public async Task UpdateEquipement_ShouldNotRecordStatus_WhenStatusIsUnchanged()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement("a");
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.UpdateEquipement(equipement.id_equipement, new UpdateEquipementDto { status_equipement = EquipementStatus.Operational });

            _equipementStatusService.Verify(s => s.CreateEquipementStatus(It.IsAny<CreateEquipementStatusDto>()), Times.Never);
        }

        [Fact]
        public async Task UpdateEquipement_ShouldReplaceImage_AndDeleteOldFiles()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement("a");
            equipement.url_picture_equipement = "equipementImages/1/old.png";
            equipement.url_thumbnail_equipement = "equipementImagesThumbnails/1/old.png";
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            SetupImageSaving();
            var service = CreateService(context);

            var result = await service.UpdateEquipement(equipement.id_equipement, new UpdateEquipementDto { img_file = BuildImageFile().Object });

            Assert.Equal("equipementImages/1/pic.png", result.url_picture_equipement);
            _fileService.Verify(f => f.DeleteFile("equipementImages/1/old.png"), Times.Once);
            _fileService.Verify(f => f.DeleteFile("equipementImagesThumbnails/1/old.png"), Times.Once);
        }

        [Fact]
        public async Task UpdateEquipement_ShouldAddImage_WhenEquipementHadNone()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement("a");
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            SetupImageSaving();
            var service = CreateService(context);

            var result = await service.UpdateEquipement(equipement.id_equipement, new UpdateEquipementDto { img_file = BuildImageFile().Object });

            Assert.NotNull(result.url_picture_equipement);
            _fileService.Verify(f => f.DeleteFile(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateEquipement_ShouldRemoveImage_WhenUnsetRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement("a");
            equipement.url_picture_equipement = "equipementImages/1/old.png";
            equipement.url_thumbnail_equipement = "equipementImagesThumbnails/1/old.png";
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateEquipement(equipement.id_equipement, new UpdateEquipementDto { unset_img_equipement = true });

            Assert.Null(result.url_picture_equipement);
            Assert.Null(result.url_thumbnail_equipement);
            _fileService.Verify(f => f.DeleteFile("equipementImages/1/old.png"), Times.Once);
            _fileService.Verify(f => f.DeleteFile("equipementImagesThumbnails/1/old.png"), Times.Once);
        }
    }
}
