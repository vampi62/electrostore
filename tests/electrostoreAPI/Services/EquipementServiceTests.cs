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
    }
}
