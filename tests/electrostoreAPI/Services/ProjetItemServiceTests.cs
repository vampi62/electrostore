using Microsoft.EntityFrameworkCore;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.ProjetItemService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class ProjetItemServiceTests : TestBase
    {
        private ProjetItemService CreateService(ApplicationDbContext context)
        {
            return new ProjetItemService(_mapper, context);
        }

        private static Projets BuildProjet(int id, string name = "Projet")
        {
            return new Projets
            {
                id_projet = id,
                nom_projet = name
            };
        }

        private static Items BuildItem(int id, string name = "Item")
        {
            return new Items
            {
                id_item = id,
                reference_name_item = "REF-" + id,
                friendly_name_item = name
            };
        }

        private static ProjetsItems BuildProjetItem(int projetId, int itemId, int qty = 3)
        {
            return new ProjetsItems
            {
                id_projet = projetId,
                id_item = itemId,
                qte_projet_item = qty
            };
        }

        // --- GetProjetItemsByProjetId ---

        [Fact]
        public async Task GetProjetItemsByProjetId_ShouldThrowKeyNotFoundException_WhenProjetNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetProjetItemsByProjetId(1);
            });
        }

        [Fact]
        public async Task GetProjetItemsByProjetId_ShouldReturnOnlyItemsForGivenProjet()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Projets.Add(BuildProjet(2));
            context.Items.Add(BuildItem(1));
            context.ProjetsItems.Add(BuildProjetItem(1, 1));
            context.ProjetsItems.Add(BuildProjetItem(2, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetProjetItemsByProjetId(1);
            // Assert
            var entry = Assert.Single(result.data);
            Assert.Equal(1, entry.id_projet);
        }

        // --- GetProjetItemsByItemId ---

        [Fact]
        public async Task GetProjetItemsByItemId_ShouldThrowKeyNotFoundException_WhenItemNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetProjetItemsByItemId(1);
            });
        }

        [Fact]
        public async Task GetProjetItemsByItemId_ShouldReturnOnlyProjetsForGivenItem()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Items.Add(BuildItem(1));
            context.Items.Add(BuildItem(2));
            context.ProjetsItems.Add(BuildProjetItem(1, 1));
            context.ProjetsItems.Add(BuildProjetItem(1, 2));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetProjetItemsByItemId(1);
            // Assert
            var entry = Assert.Single(result.data);
            Assert.Equal(1, entry.id_item);
        }

        // --- GetProjetItemById ---

        [Fact]
        public async Task GetProjetItemById_ShouldReturnEntry_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Items.Add(BuildItem(1));
            context.ProjetsItems.Add(BuildProjetItem(1, 1, 9));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetProjetItemById(1, 1);
            // Assert
            Assert.Equal(9, result.qte_projet_item);
        }

        [Fact]
        public async Task GetProjetItemById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetProjetItemById(999, 999);
            });
        }

        // --- CreateProjetItem ---

        [Fact]
        public async Task CreateProjetItem_ShouldThrowKeyNotFoundException_WhenProjetNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateProjetItemDto { id_projet = 999, id_item = 1, qte_projet_item = 1 };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreateProjetItem(dto);
            });
        }

        [Fact]
        public async Task CreateProjetItem_ShouldThrowKeyNotFoundException_WhenItemNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateProjetItemDto { id_projet = 1, id_item = 999, qte_projet_item = 1 };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreateProjetItem(dto);
            });
        }

        [Fact]
        public async Task CreateProjetItem_ShouldThrowInvalidOperationException_WhenAlreadyExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Items.Add(BuildItem(1));
            context.ProjetsItems.Add(BuildProjetItem(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateProjetItemDto { id_projet = 1, id_item = 1, qte_projet_item = 1 };
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.CreateProjetItem(dto);
            });
        }

        [Fact]
        public async Task CreateProjetItem_ShouldCreateEntry_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Items.Add(BuildItem(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateProjetItemDto { id_projet = 1, id_item = 1, qte_projet_item = 4 };
            // Act
            var result = await service.CreateProjetItem(dto);
            // Assert
            Assert.Equal(4, result.qte_projet_item);
            Assert.Equal(1, await context.ProjetsItems.CountAsync());
        }

        // --- CreateBulkProjetItem ---

        [Fact]
        public async Task CreateBulkProjetItem_ShouldReturnMixedResults_WhenOneItemMissing()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Items.Add(BuildItem(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dtos = new List<CreateProjetItemDto>
            {
                new() { id_projet = 1, id_item = 1, qte_projet_item = 1 },
                new() { id_projet = 1, id_item = 999, qte_projet_item = 1 }
            };
            // Act
            var result = await service.CreateBulkProjetItem(dtos);
            // Assert
            Assert.Single(result.Valide);
            Assert.Single(result.Error);
        }

        // --- UpdateProjetItem ---

        [Fact]
        public async Task UpdateProjetItem_ShouldThrowKeyNotFoundException_WhenProjetNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateProjetItem(999, 999, new UpdateProjetItemDto());
            });
        }

        [Fact]
        public async Task UpdateProjetItem_ShouldThrowKeyNotFoundException_WhenEntryNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Items.Add(BuildItem(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateProjetItem(1, 1, new UpdateProjetItemDto());
            });
        }

        [Fact]
        public async Task UpdateProjetItem_ShouldUpdateQuantity_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Items.Add(BuildItem(1));
            context.ProjetsItems.Add(BuildProjetItem(1, 1, 3));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.UpdateProjetItem(1, 1, new UpdateProjetItemDto { qte_projet_item = 6 });
            // Assert
            Assert.Equal(6, result.qte_projet_item);
        }

        // --- DeleteProjetItem ---

        [Fact]
        public async Task DeleteProjetItem_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeleteProjetItem(999, 999);
            });
        }

        [Fact]
        public async Task DeleteProjetItem_ShouldDeleteEntry_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projets.Add(BuildProjet(1));
            context.Items.Add(BuildItem(1));
            context.ProjetsItems.Add(BuildProjetItem(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            await service.DeleteProjetItem(1, 1);
            // Assert
            Assert.Equal(0, await context.ProjetsItems.CountAsync());
        }
    }
}
