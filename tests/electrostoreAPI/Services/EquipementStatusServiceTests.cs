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
using ElectrostoreAPI.Services.EquipementStatusService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class EquipementStatusServiceTests : TestBase
    {
        private EquipementStatusService CreateService(ApplicationDbContext context) =>
            new(_mapper, context);

        private static Equipements BuildEquipement(string reference = "eq") => new()
        {
            reference_name_equipement = reference,
            friendly_name_equipement = reference
        };

        // --- GetEquipementStatusByEquipementId ---

        [Fact]
        public async Task GetEquipementStatusByEquipementId_ShouldReturnHistoryForEquipement()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            context.EquipementsStatus.Add(new EquipementsStatus { id_equipement = equipement.id_equipement, status_equipement = EquipementStatus.Operational });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetEquipementStatusByEquipementId(equipement.id_equipement);

            Assert.Single(result.data);
        }

        [Fact]
        public async Task GetEquipementStatusByEquipementId_ShouldThrowKeyNotFoundException_WhenEquipementDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetEquipementStatusByEquipementId(999));
        }

        // --- GetEquipementStatusById ---

        [Fact]
        public async Task GetEquipementStatusById_ShouldReturnStatus_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            var status = new EquipementsStatus { id_equipement = equipement.id_equipement, status_equipement = EquipementStatus.OutOfService };
            context.EquipementsStatus.Add(status);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetEquipementStatusById(status.id_equipement_status);

            Assert.Equal(status.id_equipement_status, result.id_equipement_status);
            Assert.Equal(EquipementStatus.OutOfService, result.status_equipement);
        }

        [Fact]
        public async Task GetEquipementStatusById_ShouldThrowKeyNotFoundException_WhenEquipementIdDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            var status = new EquipementsStatus { id_equipement = equipement.id_equipement, status_equipement = EquipementStatus.OutOfService };
            context.EquipementsStatus.Add(status);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetEquipementStatusById(status.id_equipement_status, equipement.id_equipement + 1));
        }

        [Fact]
        public async Task GetEquipementStatusById_ShouldThrowKeyNotFoundException_WhenStatusDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetEquipementStatusById(999));
        }

        // --- CreateEquipementStatus ---

        [Fact]
        public async Task CreateEquipementStatus_ShouldPersistStatus_WhenEquipementExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateEquipementStatusDto { id_equipement = equipement.id_equipement, status_equipement = EquipementStatus.InMaintenance };

            var result = await service.CreateEquipementStatus(dto);

            Assert.Equal(EquipementStatus.InMaintenance, result.status_equipement);
            Assert.Equal(1, await context.EquipementsStatus.CountAsync());
        }

        [Fact]
        public async Task CreateEquipementStatus_ShouldThrowKeyNotFoundException_WhenEquipementDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateEquipementStatusDto { id_equipement = 999, status_equipement = EquipementStatus.Operational };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateEquipementStatus(dto));
        }
    }
}
