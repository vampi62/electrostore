using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using electrostore;
using electrostore.Dto;
using electrostore.Enums;
using electrostore.Models;
using electrostore.Services.ProjetCommentaireService;
using electrostore.Services.SessionService;
using electrostore.Tests.Utils;

namespace electrostore.Tests.Services
{
    public class ProjetCommentaireServiceTests : TestBase
    {
        private readonly Mock<ISessionService> _sessionService;

        public ProjetCommentaireServiceTests()
        {
            _sessionService = new Mock<ISessionService>();
            _sessionService.Setup(s => s.GetClientIp()).Returns("127.0.0.1");
            _sessionService.Setup(s => s.GetTokenId()).Returns("token");
            _sessionService.Setup(s => s.GetTokenAuthMethod()).Returns("pwd");
        }

        [Fact]
        public async Task GetByProjet_ShouldReturnCommentaires_AndCount()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Projets.Add(new Projets
            {
                id_projet = 1,
                nom_projet = "P1",
                description_projet = "D1",
                url_projet = "https://ex.com/p/1",
            });
            context.Users.Add(new Users
            {
                id_user = 10,
                email_user = "u@test.com",
                prenom_user = "U",
                nom_user = "S",
                mdp_user = "hashedpwd",
                role_user = UserRole.User
            });
            for (int i = 1; i <= 3; i++)
            {
                context.ProjetsCommentaires.Add(new ProjetsCommentaires
                {
                    id_projet_commentaire = i,
                    id_projet = 1,
                    id_user = 10,
                    contenu_projet_commentaire = $"C{i}",
                    created_at = DateTime.UtcNow.AddMinutes(-i),
                    updated_at = DateTime.UtcNow.AddMinutes(-i)
                });
            }
            await context.SaveChangesAsync();

            var service = new ProjetCommentaireService(_mapper, context, _sessionService.Object);

            var list = await service.GetProjetCommentairesByProjetId(1, 10, 0);
            var count = await service.GetProjetCommentairesCountByProjetId(1);

            Assert.NotNull(list);
            Assert.Equal(3, list.Count());
            Assert.Equal(3, count);
        }

        [Fact]
        public async Task GetByUser_ShouldReturnCommentaires_AndCount()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Projets.Add(new Projets
            {
                id_projet = 2,
                nom_projet = "P2",
                description_projet = "D2",
                url_projet = "https://ex.com/p/2",
            });
            context.Users.Add(new Users
            {
                id_user = 21,
                email_user = "u2@test.com",
                prenom_user = "U2",
                nom_user = "S2",
                mdp_user = "hashedpwd",
                role_user = UserRole.User
            });
            for (int i = 1; i <= 2; i++)
            {
                context.ProjetsCommentaires.Add(new ProjetsCommentaires
                {
                    id_projet_commentaire = 100 + i,
                    id_projet = 2,
                    id_user = 21,
                    contenu_projet_commentaire = $"UC{i}",
                    created_at = DateTime.UtcNow.AddMinutes(-i),
                    updated_at = DateTime.UtcNow.AddMinutes(-i)
                });
            }
            await context.SaveChangesAsync();

            var service = new ProjetCommentaireService(_mapper, context, _sessionService.Object);

            var list = await service.GetProjetCommentairesByUserId(21, 10, 0);
            var count = await service.GetProjetCommentairesCountByUserId(21);

            Assert.NotNull(list);
            Assert.Equal(2, list.Count());
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task GetById_ShouldReturnSingleCommentaire()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Projets.Add(new Projets
            {
                id_projet = 3,
                nom_projet = "P3",
                description_projet = "D3",
                url_projet = "https://ex.com/p/3",
            });
            context.Users.Add(new Users
            {
                id_user = 31,
                email_user = "g@test.com",
                prenom_user = "G",
                nom_user = "O",
                mdp_user = "hashedpwd",
                role_user = UserRole.User
            });
            context.ProjetsCommentaires.Add(new ProjetsCommentaires
            {
                id_projet_commentaire = 200,
                id_projet = 3,
                id_user = 31,
                contenu_projet_commentaire = "Hello",
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new ProjetCommentaireService(_mapper, context, _sessionService.Object);

            var result = await service.GetProjetCommentairesById(200);

            Assert.NotNull(result);
            Assert.Equal(200, result.id_projet_commentaire);
            Assert.Equal(3, result.id_projet);
            Assert.Equal(31, result.id_user);
            Assert.Equal("Hello", result.contenu_projet_commentaire);
        }

        [Fact]
        public async Task Create_ShouldCreateCommentaire()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Projets.Add(new Projets
            {
                id_projet = 4,
                nom_projet = "P4",
                description_projet = "D4",
                url_projet = "https://ex.com/p/4",
            });
            context.Users.Add(new Users
            {
                id_user = 41,
                email_user = "c@test.com",
                prenom_user = "A",
                nom_user = "B",
                mdp_user = "hashedpwd",
                role_user = UserRole.User
            });
            await context.SaveChangesAsync();

            var service = new ProjetCommentaireService(_mapper, context, _sessionService.Object);

            var dto = new CreateProjetCommentaireDto
            {
                id_projet = 4,
                id_user = 41,
                contenu_projet_commentaire = "New"
            };

            var created = await service.CreateProjetCommentaire(dto);

            Assert.NotNull(created);
            Assert.Equal(4, created.id_projet);
            Assert.Equal(41, created.id_user);
            Assert.Equal("New", created.contenu_projet_commentaire);

            var inDb = await context.ProjetsCommentaires.FirstOrDefaultAsync(p => p.id_projet == 4 && p.id_user == 41);
            Assert.NotNull(inDb);
        }

        [Fact]
        public async Task Update_ShouldUpdate_WhenAuthorized()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Projets.Add(new Projets
            {
                id_projet = 5,
                nom_projet = "P5",
                description_projet = "D5",
                url_projet = "https://ex.com/p/5",
            });
            context.Users.Add(new Users
            {
                id_user = 51,
                email_user = "u@test.com",
                prenom_user = "U",
                nom_user = "S",
                mdp_user = "hashedpwd",
                role_user = UserRole.User
            });
            context.ProjetsCommentaires.Add(new ProjetsCommentaires
            {
                id_projet_commentaire = 300,
                id_projet = 5,
                id_user = 51,
                contenu_projet_commentaire = "Old",
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            _sessionService.Setup(s => s.GetClientId()).Returns(51);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);

            var service = new ProjetCommentaireService(_mapper, context, _sessionService.Object);

            var updated = await service.UpdateProjetCommentaire(300, new UpdateProjetCommentaireDto
            {
                contenu_projet_commentaire = "Updated"
            });

            Assert.NotNull(updated);
            Assert.Equal("Updated", updated.contenu_projet_commentaire);

            var inDb = await context.ProjetsCommentaires.FindAsync(300);
            Assert.NotNull(inDb);
            Assert.Equal("Updated", inDb!.contenu_projet_commentaire);
        }

        [Fact]
        public async Task Delete_ShouldDelete_WhenAuthorized()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Projets.Add(new Projets
            {
                id_projet = 6,
                nom_projet = "P6",
                description_projet = "D6",
                url_projet = "https://ex.com/p/6",
            });
            context.Users.Add(new Users
            {
                id_user = 61,
                email_user = "d@test.com",
                prenom_user = "D",
                nom_user = "E",
                mdp_user = "hashedpwd",
                role_user = UserRole.User
            });
            context.ProjetsCommentaires.Add(new ProjetsCommentaires
            {
                id_projet_commentaire = 400,
                id_projet = 6,
                id_user = 61,
                contenu_projet_commentaire = "ToDelete",
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            _sessionService.Setup(s => s.GetClientId()).Returns(61);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);

            var service = new ProjetCommentaireService(_mapper, context, _sessionService.Object);

            await service.DeleteProjetCommentaire(400);

            var inDb = await context.ProjetsCommentaires.FindAsync(400);
            Assert.Null(inDb);
        }
    }
}