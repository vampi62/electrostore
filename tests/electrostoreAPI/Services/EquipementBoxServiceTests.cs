using Microsoft.EntityFrameworkCore;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.EquipementBoxService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class EquipementBoxServiceTests : TestBase
    {
        private EquipementBoxService CreateService(ApplicationDbContext context) =>
            new(_mapper, context);

        private static Stores BuildStore(string name = "store") => new()
        {
            name_store = name,
            mqtt_name_store = name + "-mqtt"
        };

        private static Boxs BuildBox(int storeId) => new()
        {
            id_store = storeId,
            xstart_box = 0,
            ystart_box = 0,
            xend_box = 5,
            yend_box = 5
        };

        private static Equipements BuildEquipement(string reference = "eq") => new()
        {
            reference_name_equipement = reference,
            friendly_name_equipement = reference
        };

        // --- GetEquipementsBoxsByBoxId ---

        [Fact]
        public async Task GetEquipementsBoxsByBoxId_ShouldReturnEquipementsLinkedToBox()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = BuildBox(store.id_store);
            var equipement = BuildEquipement();
            context.Boxs.Add(box);
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            context.EquipementsBoxs.Add(new EquipementsBoxs { id_box = box.id_box, id_equipement = equipement.id_equipement });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetEquipementsBoxsByBoxId(box.id_box);

            var single = Assert.Single(result.data);
            Assert.Equal(equipement.id_equipement, single.id_equipement);
        }

        [Fact]
        public async Task GetEquipementsBoxsByBoxId_ShouldThrowKeyNotFoundException_WhenBoxDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetEquipementsBoxsByBoxId(999));
        }

        // --- GetEquipementsBoxsByEquipementId ---

        [Fact]
        public async Task GetEquipementsBoxsByEquipementId_ShouldReturnBoxesLinkedToEquipement()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = BuildBox(store.id_store);
            var equipement = BuildEquipement();
            context.Boxs.Add(box);
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            context.EquipementsBoxs.Add(new EquipementsBoxs { id_box = box.id_box, id_equipement = equipement.id_equipement });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetEquipementsBoxsByEquipementId(equipement.id_equipement);

            var single = Assert.Single(result.data);
            Assert.Equal(box.id_box, single.id_box);
        }

        [Fact]
        public async Task GetEquipementsBoxsByEquipementId_ShouldThrowKeyNotFoundException_WhenEquipementDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetEquipementsBoxsByEquipementId(999));
        }

        // --- GetEquipementBoxById ---

        [Fact]
        public async Task GetEquipementBoxById_ShouldReturnEquipementBox_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = BuildBox(store.id_store);
            var equipement = BuildEquipement();
            context.Boxs.Add(box);
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            context.EquipementsBoxs.Add(new EquipementsBoxs { id_box = box.id_box, id_equipement = equipement.id_equipement });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetEquipementBoxById(equipement.id_equipement, box.id_box);

            Assert.Equal(box.id_box, result.id_box);
            Assert.Equal(equipement.id_equipement, result.id_equipement);
        }

        [Fact]
        public async Task GetEquipementBoxById_ShouldThrowKeyNotFoundException_WhenLinkDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetEquipementBoxById(1, 1));
        }

        // --- CreateEquipementBox ---

        [Fact]
        public async Task CreateEquipementBox_ShouldPersistLink_WhenBoxAndEquipementExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = BuildBox(store.id_store);
            var equipement = BuildEquipement();
            context.Boxs.Add(box);
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateEquipementBoxDto { id_box = box.id_box, id_equipement = equipement.id_equipement };

            var result = await service.CreateEquipementBox(dto);

            Assert.Equal(box.id_box, result.id_box);
            Assert.Equal(1, await context.EquipementsBoxs.CountAsync());
        }

        [Fact]
        public async Task CreateEquipementBox_ShouldThrowKeyNotFoundException_WhenBoxDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateEquipementBoxDto { id_box = 999, id_equipement = equipement.id_equipement };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateEquipementBox(dto));
        }

        [Fact]
        public async Task CreateEquipementBox_ShouldThrowKeyNotFoundException_WhenEquipementDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = BuildBox(store.id_store);
            context.Boxs.Add(box);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateEquipementBoxDto { id_box = box.id_box, id_equipement = 999 };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateEquipementBox(dto));
        }

        [Fact]
        public async Task CreateEquipementBox_ShouldThrowInvalidOperationException_WhenEquipementAlreadyAssignedToABox()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box1 = BuildBox(store.id_store);
            var box2 = new Boxs { id_store = store.id_store, xstart_box = 6, ystart_box = 6, xend_box = 10, yend_box = 10 };
            var equipement = BuildEquipement();
            context.Boxs.AddRange(box1, box2);
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            context.EquipementsBoxs.Add(new EquipementsBoxs { id_box = box1.id_box, id_equipement = equipement.id_equipement });
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateEquipementBoxDto { id_box = box2.id_box, id_equipement = equipement.id_equipement };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateEquipementBox(dto));
        }

        // --- DeleteEquipementBox ---

        [Fact]
        public async Task DeleteEquipementBox_ShouldRemoveLink_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var store = BuildStore();
            context.Stores.Add(store);
            await context.SaveChangesAsync();
            var box = BuildBox(store.id_store);
            var equipement = BuildEquipement();
            context.Boxs.Add(box);
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            context.EquipementsBoxs.Add(new EquipementsBoxs { id_box = box.id_box, id_equipement = equipement.id_equipement });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.DeleteEquipementBox(equipement.id_equipement, box.id_box);

            Assert.Equal(0, await context.EquipementsBoxs.CountAsync());
        }

        [Fact]
        public async Task DeleteEquipementBox_ShouldThrowKeyNotFoundException_WhenLinkDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteEquipementBox(1, 1));
        }
    }
}
