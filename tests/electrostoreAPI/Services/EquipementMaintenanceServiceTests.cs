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
using ElectrostoreAPI.Services.EquipementMaintenanceService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class EquipementMaintenanceServiceTests : TestBase
    {
        private EquipementMaintenanceService CreateService(ApplicationDbContext context) =>
            new(_mapper, context);

        private static Equipements BuildEquipement(string reference = "eq") => new()
        {
            reference_name_equipement = reference,
            friendly_name_equipement = reference
        };

        private static Users BuildUser(string email = "a@test.com") => new()
        {
            name_user = "Nom",
            firstname_user = "Prenom",
            email_user = email,
            password_user = "hashed"
        };

        // --- GetEquipementsMaintenancesByEquipementId ---

        [Fact]
        public async Task GetEquipementsMaintenancesByEquipementId_ShouldReturnMaintenancesForEquipement()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            context.EquipementsMaintenances.Add(new EquipementsMaintenances
            {
                id_equipement = equipement.id_equipement,
                type_equipement_maintenance = EquipementMaintenanceType.Preventive,
                date_planned_equipement_maintenance = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetEquipementsMaintenancesByEquipementId(equipement.id_equipement);

            Assert.Single(result.data);
        }

        [Fact]
        public async Task GetEquipementsMaintenancesByEquipementId_ShouldThrowKeyNotFoundException_WhenEquipementDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetEquipementsMaintenancesByEquipementId(999));
        }

        // --- GetEquipementMaintenanceById ---

        [Fact]
        public async Task GetEquipementMaintenanceById_ShouldReturnMaintenance_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            var maintenance = new EquipementsMaintenances
            {
                id_equipement = equipement.id_equipement,
                type_equipement_maintenance = EquipementMaintenanceType.Corrective,
                date_planned_equipement_maintenance = DateTime.UtcNow
            };
            context.EquipementsMaintenances.Add(maintenance);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetEquipementMaintenanceById(maintenance.id_equipement_maintenance);

            Assert.Equal(maintenance.id_equipement_maintenance, result.id_equipement_maintenance);
        }

        [Fact]
        public async Task GetEquipementMaintenanceById_ShouldThrowKeyNotFoundException_WhenEquipementIdDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            var maintenance = new EquipementsMaintenances
            {
                id_equipement = equipement.id_equipement,
                type_equipement_maintenance = EquipementMaintenanceType.Corrective,
                date_planned_equipement_maintenance = DateTime.UtcNow
            };
            context.EquipementsMaintenances.Add(maintenance);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetEquipementMaintenanceById(maintenance.id_equipement_maintenance, equipement.id_equipement + 1));
        }

        [Fact]
        public async Task GetEquipementMaintenanceById_ShouldThrowKeyNotFoundException_WhenMaintenanceDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetEquipementMaintenanceById(999));
        }

        // --- CreateEquipementMaintenance ---

        [Fact]
        public async Task CreateEquipementMaintenance_ShouldPersistMaintenance_WhenEquipementExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateEquipementMaintenanceDto
            {
                id_equipement = equipement.id_equipement,
                type_equipement_maintenance = EquipementMaintenanceType.Inspection,
                date_planned_equipement_maintenance = DateTime.UtcNow
            };

            var result = await service.CreateEquipementMaintenance(dto);

            Assert.Equal(EquipementMaintenanceType.Inspection, result.type_equipement_maintenance);
            Assert.Equal(1, await context.EquipementsMaintenances.CountAsync());
        }

        [Fact]
        public async Task CreateEquipementMaintenance_ShouldThrowKeyNotFoundException_WhenEquipementDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateEquipementMaintenanceDto
            {
                id_equipement = 999,
                type_equipement_maintenance = EquipementMaintenanceType.Inspection,
                date_planned_equipement_maintenance = DateTime.UtcNow
            };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateEquipementMaintenance(dto));
        }

        [Fact]
        public async Task CreateEquipementMaintenance_ShouldPersistMaintenance_WhenUserExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            var user = BuildUser();
            context.Equipements.Add(equipement);
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateEquipementMaintenanceDto
            {
                id_equipement = equipement.id_equipement,
                id_user = user.id_user,
                type_equipement_maintenance = EquipementMaintenanceType.Inspection,
                date_planned_equipement_maintenance = DateTime.UtcNow
            };

            var result = await service.CreateEquipementMaintenance(dto);

            Assert.Equal(user.id_user, result.id_user);
        }

        [Fact]
        public async Task CreateEquipementMaintenance_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateEquipementMaintenanceDto
            {
                id_equipement = equipement.id_equipement,
                id_user = 999,
                type_equipement_maintenance = EquipementMaintenanceType.Inspection,
                date_planned_equipement_maintenance = DateTime.UtcNow
            };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateEquipementMaintenance(dto));
        }

        // --- UpdateEquipementMaintenance ---

        [Fact]
        public async Task UpdateEquipementMaintenance_ShouldUpdateProvidedFields()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            var maintenance = new EquipementsMaintenances
            {
                id_equipement = equipement.id_equipement,
                type_equipement_maintenance = EquipementMaintenanceType.Preventive,
                date_planned_equipement_maintenance = DateTime.UtcNow
            };
            context.EquipementsMaintenances.Add(maintenance);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var doneDate = DateTime.UtcNow;
            var dto = new UpdateEquipementMaintenanceDto { date_done_equipement_maintenance = doneDate, description_equipement_maintenance = "done" };

            var result = await service.UpdateEquipementMaintenance(maintenance.id_equipement_maintenance, dto);

            Assert.Equal(doneDate, result.date_done_equipement_maintenance);
            Assert.Equal("done", result.description_equipement_maintenance);
        }

        [Fact]
        public async Task UpdateEquipementMaintenance_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            var maintenance = new EquipementsMaintenances
            {
                id_equipement = equipement.id_equipement,
                type_equipement_maintenance = EquipementMaintenanceType.Preventive,
                date_planned_equipement_maintenance = DateTime.UtcNow
            };
            context.EquipementsMaintenances.Add(maintenance);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new UpdateEquipementMaintenanceDto { id_user = 999 };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateEquipementMaintenance(maintenance.id_equipement_maintenance, dto));
        }

        [Fact]
        public async Task UpdateEquipementMaintenance_ShouldThrowKeyNotFoundException_WhenEquipementIdDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            var maintenance = new EquipementsMaintenances
            {
                id_equipement = equipement.id_equipement,
                type_equipement_maintenance = EquipementMaintenanceType.Preventive,
                date_planned_equipement_maintenance = DateTime.UtcNow
            };
            context.EquipementsMaintenances.Add(maintenance);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateEquipementMaintenance(
                maintenance.id_equipement_maintenance, new UpdateEquipementMaintenanceDto(), equipement.id_equipement + 1));
        }

        [Fact]
        public async Task UpdateEquipementMaintenance_ShouldThrowKeyNotFoundException_WhenMaintenanceDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateEquipementMaintenance(999, new UpdateEquipementMaintenanceDto()));
        }

        // --- DeleteEquipementMaintenance ---

        [Fact]
        public async Task DeleteEquipementMaintenance_ShouldRemoveMaintenance_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            var maintenance = new EquipementsMaintenances
            {
                id_equipement = equipement.id_equipement,
                type_equipement_maintenance = EquipementMaintenanceType.Preventive,
                date_planned_equipement_maintenance = DateTime.UtcNow
            };
            context.EquipementsMaintenances.Add(maintenance);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.DeleteEquipementMaintenance(maintenance.id_equipement_maintenance);

            Assert.Equal(0, await context.EquipementsMaintenances.CountAsync());
        }

        [Fact]
        public async Task DeleteEquipementMaintenance_ShouldThrowKeyNotFoundException_WhenEquipementIdDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            var maintenance = new EquipementsMaintenances
            {
                id_equipement = equipement.id_equipement,
                type_equipement_maintenance = EquipementMaintenanceType.Preventive,
                date_planned_equipement_maintenance = DateTime.UtcNow
            };
            context.EquipementsMaintenances.Add(maintenance);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteEquipementMaintenance(maintenance.id_equipement_maintenance, equipement.id_equipement + 1));
        }

        [Fact]
        public async Task DeleteEquipementMaintenance_ShouldThrowKeyNotFoundException_WhenMaintenanceDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteEquipementMaintenance(999));
        }
    }
}
