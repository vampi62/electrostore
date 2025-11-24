using Microsoft.EntityFrameworkCore;
using Xunit;
using electrostore;
using electrostore.Dto;
using electrostore.Models;
using electrostore.Services.ProjetTagService;
using electrostore.Tests.Utils;

namespace electrostore.Tests.Services
{
    public class ProjetTagServiceTests : TestBase
    {
        [Fact]
        public async Task GetList_AndCount_ShouldWork_WithIdFilter()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.ProjetTags.AddRange(
                new ProjetTags { id_projet_tag = 1, nom_projet_tag = "Tag1", poids_projet_tag = 1 },
                new ProjetTags { id_projet_tag = 2, nom_projet_tag = "Tag2", poids_projet_tag = 2 },
                new ProjetTags { id_projet_tag = 3, nom_projet_tag = "Tag3", poids_projet_tag = 3 }
            );
            await context.SaveChangesAsync();

            var service = new ProjetTagService(_mapper, context);

            var listAll = await service.GetProjetTags(10, 0);
            var count = await service.GetProjetTagsCount();

            Assert.Equal(3, listAll.Count());
            Assert.Equal(3, count);

            var filtered = await service.GetProjetTags(10, 0, idResearch: new List<int> { 1, 3 });
            Assert.Equal(2, filtered.Count());
            Assert.Contains(filtered, t => t.id_projet_tag == 1);
            Assert.Contains(filtered, t => t.id_projet_tag == 3);
        }

        [Fact]
        public async Task GetById_ShouldReturnExtended_WithCounts()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            // Seed tag + two linked projets
            context.ProjetTags.Add(new ProjetTags { id_projet_tag = 10, nom_projet_tag = "PT10", poids_projet_tag = 0 });
            context.Projets.AddRange(
                new Projets { id_projet = 1, nom_projet = "P1", description_projet = "D1", url_projet = "https://ex.com/p/1" },
                new Projets { id_projet = 2, nom_projet = "P2", description_projet = "D2", url_projet = "https://ex.com/p/2" }
            );
            context.ProjetsProjetTags.AddRange(
                new ProjetsProjetTags { id_projet = 1, id_projet_tag = 10 },
                new ProjetsProjetTags { id_projet = 2, id_projet_tag = 10 }
            );
            await context.SaveChangesAsync();

            var service = new ProjetTagService(_mapper, context);
            var tag = await service.GetProjetTagById(10, expand: new List<string> { "stores_tags" });

            Assert.NotNull(tag);
            Assert.Equal(10, tag.id_projet_tag);
            Assert.Equal(2, tag.projets_projet_tags_count);
            Assert.NotNull(tag.projets_projet_tags);
            Assert.Equal(2, tag.projets_projet_tags!.Count());
        }

        [Fact]
        public async Task Create_ShouldCreate_AndPreventDuplicateNames()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.ProjetTags.Add(new ProjetTags { id_projet_tag = 20, nom_projet_tag = "Dup", poids_projet_tag = 0 });
            await context.SaveChangesAsync();

            var service = new ProjetTagService(_mapper, context);

            var created = await service.CreateProjetTag(new CreateProjetTagDto { nom_projet_tag = "New", poids_projet_tag = 5 });
            Assert.NotNull(created);
            Assert.Equal("New", created.nom_projet_tag);
            Assert.Equal(5, created.poids_projet_tag);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.CreateProjetTag(new CreateProjetTagDto { nom_projet_tag = "Dup", poids_projet_tag = 1 });
            });
        }

        [Fact]
        public async Task CreateBulk_ShouldReturnValidAndErrorLists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.ProjetTags.Add(new ProjetTags { id_projet_tag = 30, nom_projet_tag = "X", poids_projet_tag = 0 });
            await context.SaveChangesAsync();

            var service = new ProjetTagService(_mapper, context);

            var bulk = new List<CreateProjetTagDto>
            {
                new() { nom_projet_tag = "A", poids_projet_tag = 1 }, // valid
                new() { nom_projet_tag = "X", poids_projet_tag = 2 }  // duplicate name -> error
            };

            var result = await service.CreateBulkProjetTag(bulk);

            Assert.Single(result.Valide);
            Assert.Single(result.Error);
            Assert.Equal("A", result.Valide[0].nom_projet_tag);
        }

        [Fact]
        public async Task Update_ShouldModifyFields_AndPreventDuplicateName()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.ProjetTags.AddRange(
                new ProjetTags { id_projet_tag = 40, nom_projet_tag = "Old", poids_projet_tag = 1 },
                new ProjetTags { id_projet_tag = 41, nom_projet_tag = "Exists", poids_projet_tag = 2 }
            );
            await context.SaveChangesAsync();

            var service = new ProjetTagService(_mapper, context);

            var updated = await service.UpdateProjetTag(40, new UpdateProjetTagDto { nom_projet_tag = "NewName", poids_projet_tag = 10 });
            Assert.NotNull(updated);
            Assert.Equal("NewName", updated.nom_projet_tag);
            Assert.Equal(10, updated.poids_projet_tag);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.UpdateProjetTag(40, new UpdateProjetTagDto { nom_projet_tag = "Exists" });
            });
        }

        [Fact]
        public async Task Delete_ShouldRemoveProjetTag()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.ProjetTags.Add(new ProjetTags { id_projet_tag = 50, nom_projet_tag = "Del", poids_projet_tag = 0 });
            await context.SaveChangesAsync();

            var service = new ProjetTagService(_mapper, context);

            await service.DeleteProjetTag(50);

            var inDb = await context.ProjetTags.FindAsync(50);
            Assert.Null(inDb);
        }
    }
}