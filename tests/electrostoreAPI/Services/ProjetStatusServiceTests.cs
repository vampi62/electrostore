using Microsoft.EntityFrameworkCore;
using Xunit;
using electrostore;
using electrostore.Dto;
using electrostore.Enums;
using electrostore.Models;
using electrostore.Services.ProjetStatusService;
using electrostore.Tests.Utils;

namespace electrostore.Tests.Services
{
    public class ProjetStatusServiceTests : TestBase
    {
        [Fact]
        public async Task GetByProjet_ShouldReturnStatusList_AndCount()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Projets.Add(new Projets { id_projet = 1, nom_projet = "P1", description_projet = "D1", url_projet = "https://ex.com/p/1" });
            context.ProjetsStatus.AddRange(
                new ProjetsStatus { id_projet_status = 1, id_projet = 1, status_projet = ProjetStatus.NotStarted, created_at = DateTime.UtcNow.AddDays(-2), updated_at = DateTime.UtcNow.AddDays(-2) },
                new ProjetsStatus { id_projet_status = 2, id_projet = 1, status_projet = ProjetStatus.InProgress, created_at = DateTime.UtcNow.AddDays(-1), updated_at = DateTime.UtcNow.AddDays(-1) }
            );
            await context.SaveChangesAsync();

            var service = new ProjetStatusService(_mapper, context);

            var list = await service.GetProjetStatusByProjetId(1, 10, 0);
            var count = await service.GetProjetStatusCountByProjetId(1);

            Assert.NotNull(list);
            Assert.Equal(2, list.Count());
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task GetById_ShouldReturnSingleStatus()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Projets.Add(new Projets { id_projet = 2, nom_projet = "P2", description_projet = "D2", url_projet = "https://ex.com/p/2" });
            context.ProjetsStatus.Add(new ProjetsStatus { id_projet_status = 10, id_projet = 2, status_projet = ProjetStatus.InProgress });
            await context.SaveChangesAsync();

            var service = new ProjetStatusService(_mapper, context);
            var status = await service.GetProjetStatusById(10);

            Assert.NotNull(status);
            Assert.Equal(10, status.id_projet_status);
            Assert.Equal(2, status.id_projet);
            Assert.Equal(ProjetStatus.InProgress, status.status_projet);
        }

        [Fact]
        public async Task Create_ShouldCreateStatus()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Projets.Add(new Projets { id_projet = 3, nom_projet = "P3", description_projet = "D3", url_projet = "https://ex.com/p/3" });
            await context.SaveChangesAsync();

            var service = new ProjetStatusService(_mapper, context);

            var dto = new CreateProjetStatusDto { id_projet = 3, status_projet = ProjetStatus.Completed };
            var created = await service.CreateProjetStatus(dto);

            Assert.NotNull(created);
            Assert.Equal(3, created.id_projet);
            Assert.Equal(ProjetStatus.Completed, created.status_projet);

            var inDb = await context.ProjetsStatus.FirstOrDefaultAsync(s => s.id_projet == 3 && s.status_projet == ProjetStatus.Completed);
            Assert.NotNull(inDb);
        }
    }
}