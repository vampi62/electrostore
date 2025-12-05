using Microsoft.EntityFrameworkCore;
using Xunit;
using electrostore;
using electrostore.Dto;
using electrostore.Models;
using electrostore.Services.ProjetItemService;
using electrostore.Tests.Utils;

namespace electrostore.Tests.Services
{
    public class ProjetItemServiceTests : TestBase
    {
        [Fact]
        public async Task GetByProjet_ShouldReturnItems_AndCount()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            // Seed projet and items
            context.Projets.Add(new Projets
            {
                id_projet = 1,
                nom_projet = "P1",
                description_projet = "D1",
                url_projet = "https://ex.com/p/1",
            });
            for (int i = 1; i <= 3; i++)
            {
                context.Items.Add(new Items
                {
                    id_item = i,
                    reference_name_item = $"REF-{i}",
                    friendly_name_item = $"Item {i}",
                    description_item = $"Desc {i}",
                    seuil_min_item = 1
                });
                context.ProjetsItems.Add(new ProjetsItems
                {
                    id_projet = 1,
                    id_item = i,
                    qte_projet_item = 1 + i
                });
            }
            await context.SaveChangesAsync();

            var service = new ProjetItemService(_mapper, context);

            var list = await service.GetProjetItemsByProjetId(1, 10, 0);
            var count = await service.GetProjetItemsCountByProjetId(1);

            Assert.NotNull(list);
            Assert.Equal(3, list.Count());
            Assert.Equal(3, count);
        }

        [Fact]
        public async Task GetByItem_ShouldReturnProjets_AndCount()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            // Seed item and projets
            context.Items.Add(new Items
            {
                id_item = 10,
                reference_name_item = "REF-10",
                friendly_name_item = "Item 10",
                description_item = "Desc 10",
                seuil_min_item = 1
            });
            for (int p = 1; p <= 2; p++)
            {
                context.Projets.Add(new Projets
                {
                    id_projet = p,
                    nom_projet = $"P{p}",
                    description_projet = $"D{p}",
                    url_projet = $"https://ex.com/p/{p}",
                });
                context.ProjetsItems.Add(new ProjetsItems
                {
                    id_projet = p,
                    id_item = 10,
                    qte_projet_item = 2
                });
            }
            await context.SaveChangesAsync();

            var service = new ProjetItemService(_mapper, context);

            var list = await service.GetProjetItemsByItemId(10, 10, 0);
            var count = await service.GetProjetItemsCountByItemId(10);

            Assert.NotNull(list);
            Assert.Equal(2, list.Count());
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task GetById_ShouldReturnSingleProjetItem()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Projets.Add(new Projets { id_projet = 3, nom_projet = "P3", description_projet = "D3", url_projet = "https://ex.com/p/3" });
            context.Items.Add(new Items { id_item = 20, reference_name_item = "REF-20", friendly_name_item = "Item 20", description_item = "Desc 20", seuil_min_item = 1 });
            context.ProjetsItems.Add(new ProjetsItems { id_projet = 3, id_item = 20, qte_projet_item = 7 });
            await context.SaveChangesAsync();

            var service = new ProjetItemService(_mapper, context);
            var pi = await service.GetProjetItemById(3, 20);

            Assert.NotNull(pi);
            Assert.Equal(3, pi.id_projet);
            Assert.Equal(20, pi.id_item);
            Assert.Equal(7, pi.qte_projet_item);
        }

        [Fact]
        public async Task Create_ShouldCreateProjetItem()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Projets.Add(new Projets { id_projet = 4, nom_projet = "P4", description_projet = "D4", url_projet = "https://ex.com/p/4" });
            context.Items.Add(new Items { id_item = 30, reference_name_item = "REF-30", friendly_name_item = "Item 30", description_item = "Desc 30", seuil_min_item = 1 });
            await context.SaveChangesAsync();

            var service = new ProjetItemService(_mapper, context);

            var dto = new CreateProjetItemDto { id_projet = 4, id_item = 30, qte_projet_item = 5 };
            var created = await service.CreateProjetItem(dto);

            Assert.NotNull(created);
            Assert.Equal(4, created.id_projet);
            Assert.Equal(30, created.id_item);
            Assert.Equal(5, created.qte_projet_item);

            var inDb = await context.ProjetsItems.FindAsync(4, 30);
            Assert.NotNull(inDb);
        }

        [Fact]
        public async Task CreateBulk_ShouldReturnValidAndErrorLists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Projets.Add(new Projets { id_projet = 5, nom_projet = "P5", description_projet = "D5", url_projet = "https://ex.com/p/5" });
            context.Items.AddRange(
                new Items { id_item = 40, reference_name_item = "R40", friendly_name_item = "I40", description_item = "D40", seuil_min_item = 1 },
                new Items { id_item = 41, reference_name_item = "R41", friendly_name_item = "I41", description_item = "D41", seuil_min_item = 1 }
            );
            context.ProjetsItems.Add(new ProjetsItems { id_projet = 5, id_item = 41, qte_projet_item = 1 }); // duplicate case
            await context.SaveChangesAsync();

            var service = new ProjetItemService(_mapper, context);

            var bulk = new List<CreateProjetItemDto>
            {
                new() { id_projet = 5, id_item = 40, qte_projet_item = 2 }, // valid
                new() { id_projet = 5, id_item = 41, qte_projet_item = 2 }  // duplicate -> error
            };

            var result = await service.CreateBulkProjetItem(bulk);

            Assert.NotNull(result);
            Assert.Single(result.Valide);
            Assert.Single(result.Error);
            Assert.Equal(40, result.Valide[0].id_item);
        }

        [Fact]
        public async Task Update_ShouldModifyQte()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Projets.Add(new Projets { id_projet = 6, nom_projet = "P6", description_projet = "D6", url_projet = "https://ex.com/p/6" });
            context.Items.Add(new Items { id_item = 50, reference_name_item = "R50", friendly_name_item = "I50", description_item = "D50", seuil_min_item = 1 });
            context.ProjetsItems.Add(new ProjetsItems { id_projet = 6, id_item = 50, qte_projet_item = 1 });
            await context.SaveChangesAsync();

            var service = new ProjetItemService(_mapper, context);

            var updated = await service.UpdateProjetItem(6, 50, new UpdateProjetItemDto { qte_projet_item = 4 });

            Assert.NotNull(updated);
            Assert.Equal(4, updated.qte_projet_item);

            var inDb = await context.ProjetsItems.FindAsync(6, 50);
            Assert.NotNull(inDb);
            Assert.Equal(4, inDb!.qte_projet_item);
        }

        [Fact]
        public async Task Delete_ShouldRemoveProjetItem()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Projets.Add(new Projets { id_projet = 7, nom_projet = "P7", description_projet = "D7", url_projet = "https://ex.com/p/7" });
            context.Items.Add(new Items { id_item = 60, reference_name_item = "R60", friendly_name_item = "I60", description_item = "D60", seuil_min_item = 1 });
            context.ProjetsItems.Add(new ProjetsItems { id_projet = 7, id_item = 60, qte_projet_item = 3 });
            await context.SaveChangesAsync();

            var service = new ProjetItemService(_mapper, context);

            await service.DeleteProjetItem(7, 60);

            var inDb = await context.ProjetsItems.FindAsync(7, 60);
            Assert.Null(inDb);
        }
    }
}