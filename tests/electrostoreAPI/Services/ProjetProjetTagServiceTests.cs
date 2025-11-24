using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using electrostore;
using electrostore.Dto;
using electrostore.Enums;
using electrostore.Models;
using electrostore.Services.ProjetProjetTagService;
using electrostore.Services.SessionService;
using electrostore.Tests.Utils;

namespace electrostore.Tests.Services
{
    public class ProjetProjetTagServiceTests : TestBase
    {
        private readonly Mock<ISessionService> _sessionService;

        public ProjetProjetTagServiceTests()
        {
            _sessionService = new Mock<ISessionService>();
            _sessionService.Setup(s => s.GetClientIp()).Returns("127.0.0.1");
            _sessionService.Setup(s => s.GetTokenId()).Returns("token");
            _sessionService.Setup(s => s.GetTokenAuthMethod()).Returns("pwd");
        }

        [Fact]
        public async Task GetByProjet_ShouldReturnProjetTags_AndCount()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Projets.Add(new Projets { id_projet = 1, nom_projet = "P1", description_projet = "D1", url_projet = "https://ex.com/p/1" });
            for (int t = 1; t <= 3; t++)
            {
                context.ProjetTags.Add(new ProjetTags { id_projet_tag = t, nom_projet_tag = $"T{t}", poids_projet_tag = t });
                context.ProjetsProjetTags.Add(new ProjetsProjetTags { id_projet = 1, id_projet_tag = t });
            }
            await context.SaveChangesAsync();

            var service = new ProjetProjetTagService(_mapper, context, _sessionService.Object);

            var list = await service.GetProjetsProjetTagsByProjetId(1, 10, 0);
            var count = await service.GetProjetsProjetTagsCountByProjetId(1);

            Assert.NotNull(list);
            Assert.Equal(3, list.Count());
            Assert.Equal(3, count);
        }

        [Fact]
        public async Task GetByProjetTag_ShouldReturnProjets_AndCount()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.ProjetTags.Add(new ProjetTags { id_projet_tag = 10, nom_projet_tag = "PT10", poids_projet_tag = 0 });
            for (int p = 1; p <= 2; p++)
            {
                context.Projets.Add(new Projets { id_projet = p, nom_projet = $"P{p}", description_projet = $"D{p}", url_projet = $"https://ex.com/p/{p}" });
                context.ProjetsProjetTags.Add(new ProjetsProjetTags { id_projet = p, id_projet_tag = 10 });
            }
            await context.SaveChangesAsync();

            var service = new ProjetProjetTagService(_mapper, context, _sessionService.Object);

            var list = await service.GetProjetsProjetTagsByprojetTagId(10, 10, 0);
            var count = await service.GetProjetsProjetTagsCountByprojetTagId(10);

            Assert.NotNull(list);
            Assert.Equal(2, list.Count());
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task GetById_ShouldReturnSingleLink()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Projets.Add(new Projets { id_projet = 3, nom_projet = "P3", description_projet = "D3", url_projet = "https://ex.com/p/3" });
            context.ProjetTags.Add(new ProjetTags { id_projet_tag = 20, nom_projet_tag = "PT20", poids_projet_tag = 1 });
            context.ProjetsProjetTags.Add(new ProjetsProjetTags { id_projet = 3, id_projet_tag = 20 });
            await context.SaveChangesAsync();

            var service = new ProjetProjetTagService(_mapper, context, _sessionService.Object);
            var ppt = await service.GetProjetProjetTagById(3, 20);

            Assert.NotNull(ppt);
            Assert.Equal(3, ppt.id_projet);
            Assert.Equal(20, ppt.id_projet_tag);
        }

        [Fact]
        public async Task Create_ShouldCreate_WhenAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Projets.Add(new Projets { id_projet = 4, nom_projet = "P4", description_projet = "D4", url_projet = "https://ex.com/p/4" });
            context.ProjetTags.Add(new ProjetTags { id_projet_tag = 30, nom_projet_tag = "PT30", poids_projet_tag = 0 });
            await context.SaveChangesAsync();

            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Admin);

            var service = new ProjetProjetTagService(_mapper, context, _sessionService.Object);

            var dto = new CreateProjetProjetTagDto { id_projet = 4, id_projet_tag = 30 };
            var created = await service.CreateProjetProjetTag(dto);

            Assert.NotNull(created);
            Assert.Equal(4, created.id_projet);
            Assert.Equal(30, created.id_projet_tag);

            var inDb = await context.ProjetsProjetTags.FindAsync(4, 30);
            Assert.NotNull(inDb);
        }

        [Fact]
        public async Task Create_ShouldFail_WhenUnauthorized()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Projets.Add(new Projets { id_projet = 41, nom_projet = "P41", description_projet = "D41", url_projet = "https://ex.com/p/41" });
            context.ProjetTags.Add(new ProjetTags { id_projet_tag = 31, nom_projet_tag = "PT31", poids_projet_tag = 0 });
            await context.SaveChangesAsync();

            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);

            var service = new ProjetProjetTagService(_mapper, context, _sessionService.Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.CreateProjetProjetTag(new CreateProjetProjetTagDto { id_projet = 41, id_projet_tag = 31 });
            });
        }

        [Fact]
        public async Task CreateBulk_ShouldReturnValidAndErrorLists_WhenAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Projets.Add(new Projets { id_projet = 5, nom_projet = "P5", description_projet = "D5", url_projet = "https://ex.com/p/5" });
            context.ProjetTags.AddRange(
                new ProjetTags { id_projet_tag = 40, nom_projet_tag = "PT40", poids_projet_tag = 0 },
                new ProjetTags { id_projet_tag = 41, nom_projet_tag = "PT41", poids_projet_tag = 0 }
            );
            context.ProjetsProjetTags.Add(new ProjetsProjetTags { id_projet = 5, id_projet_tag = 41 }); // duplicate
            await context.SaveChangesAsync();

            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Admin);
            var service = new ProjetProjetTagService(_mapper, context, _sessionService.Object);

            var bulk = new List<CreateProjetProjetTagDto>
            {
                new() { id_projet = 5, id_projet_tag = 40 }, // valid
                new() { id_projet = 5, id_projet_tag = 41 }  // duplicate -> error
            };

            var result = await service.CreateBulkProjetProjetTag(bulk);

            Assert.Single(result.Valide);
            Assert.Single(result.Error);
            Assert.Equal(40, result.Valide[0].id_projet_tag);
        }

        [Fact]
        public async Task Delete_ShouldRemove_WhenAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Projets.Add(new Projets { id_projet = 6, nom_projet = "P6", description_projet = "D6", url_projet = "https://ex.com/p/6" });
            context.ProjetTags.Add(new ProjetTags { id_projet_tag = 50, nom_projet_tag = "PT50", poids_projet_tag = 0 });
            context.ProjetsProjetTags.Add(new ProjetsProjetTags { id_projet = 6, id_projet_tag = 50 });
            await context.SaveChangesAsync();

            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Admin);
            var service = new ProjetProjetTagService(_mapper, context, _sessionService.Object);

            await service.DeleteProjetProjetTag(6, 50);

            var inDb = await context.ProjetsProjetTags.FindAsync(6, 50);
            Assert.Null(inDb);
        }

        [Fact]
        public async Task Delete_ShouldFail_WhenUnauthorized()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Projets.Add(new Projets { id_projet = 7, nom_projet = "P7", description_projet = "D7", url_projet = "https://ex.com/p/7" });
            context.ProjetTags.Add(new ProjetTags { id_projet_tag = 60, nom_projet_tag = "PT60", poids_projet_tag = 0 });
            context.ProjetsProjetTags.Add(new ProjetsProjetTags { id_projet = 7, id_projet_tag = 60 });
            await context.SaveChangesAsync();

            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = new ProjetProjetTagService(_mapper, context, _sessionService.Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.DeleteProjetProjetTag(7, 60);
            });
        }

        [Fact]
        public async Task DeleteBulk_ShouldReturnValidAndError_WhenAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Projets.AddRange(
                new Projets { id_projet = 8, nom_projet = "P8", description_projet = "D8", url_projet = "https://ex.com/p/8" },
                new Projets { id_projet = 9, nom_projet = "P9", description_projet = "D9", url_projet = "https://ex.com/p/9" }
            );
            context.ProjetTags.AddRange(
                new ProjetTags { id_projet_tag = 70, nom_projet_tag = "PT70", poids_projet_tag = 0 },
                new ProjetTags { id_projet_tag = 71, nom_projet_tag = "PT71", poids_projet_tag = 0 }
            );
            context.ProjetsProjetTags.Add(new ProjetsProjetTags { id_projet = 8, id_projet_tag = 70 }); // existing
            await context.SaveChangesAsync();

            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Admin);
            var service = new ProjetProjetTagService(_mapper, context, _sessionService.Object);

            var bulk = new List<CreateProjetProjetTagDto>
            {
                new() { id_projet = 8, id_projet_tag = 70 }, // valid delete
                new() { id_projet = 9, id_projet_tag = 71 }  // not existing -> error
            };

            var result = await service.DeleteBulkProjetProjetTag(bulk);

            Assert.Single(result.Valide);
            Assert.Single(result.Error);
            Assert.Equal(8, result.Valide[0].id_projet);
            Assert.Equal(70, result.Valide[0].id_projet_tag);
        }
    }
}