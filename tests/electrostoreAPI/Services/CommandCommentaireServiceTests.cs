using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using electrostore;
using electrostore.Dto;
using electrostore.Enums;
using electrostore.Models;
using electrostore.Services.CommandCommentaireService;
using electrostore.Services.SessionService;
using electrostore.Tests.Utils;

namespace electrostore.Tests.Services
{
    public class CommandCommentaireServiceTests : TestBase
    {
        private readonly Mock<ISessionService> _sessionService;

        public CommandCommentaireServiceTests()
        {
            _sessionService = new Mock<ISessionService>();
            _sessionService.Setup(s => s.GetClientIp()).Returns("127.0.0.1");
            _sessionService.Setup(s => s.GetTokenId()).Returns("token");
            _sessionService.Setup(s => s.GetTokenAuthMethod()).Returns("pwd");
        }

        [Fact]
        public async Task GetByCommand_ShouldReturnCommentaires_WhenExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            // Seed command and comments
            context.Commands.Add(new Commands
            {
                id_command = 1,
                prix_command = 100,
                url_command = "https://ex.com/c/1",
                status_command = "pending",
                date_command = DateTime.UtcNow
            });
            context.Users.Add(new Users
            {
                id_user = 10,
                email_user = "user@test.com",
                nom_user = "user",
                prenom_user = "test",
                mdp_user = "hashedpwd",
                role_user = UserRole.User
            });
            for (int i = 1; i <= 3; i++)
            {
                context.CommandsCommentaires.Add(new CommandsCommentaires
                {
                    id_command_commentaire = i,
                    id_command = 1,
                    id_user = 10,
                    contenu_command_commentaire = $"Comment {i}",
                    created_at = DateTime.UtcNow.AddMinutes(-i),
                    updated_at = DateTime.UtcNow.AddMinutes(-i)
                });
            }
            await context.SaveChangesAsync();

            var commandCommentaireService = new CommandCommentaireService(_mapper, context, _sessionService.Object);

            var list = await commandCommentaireService.GetCommandsCommentairesByCommandId(1, 10, 0);
            var count = await commandCommentaireService.GetCommandsCommentairesCountByCommandId(1);

            Assert.NotNull(list);
            Assert.Equal(3, list.Count());
            Assert.Equal(3, count);
        }

        [Fact]
        public async Task GetByUser_ShouldReturnCommentaires_WhenExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            // Seed user and comments
            context.Users.Add(new Users
            {
                id_user = 21,
                email_user = "user2@test.com",
                nom_user = "user2",
                prenom_user = "test2",
                mdp_user = "hashedpwd",
                role_user = UserRole.User
            });
            context.Commands.Add(new Commands
            {
                id_command = 2,
                prix_command = 50,
                url_command = "https://ex.com/c/2",
                status_command = "pending",
                date_command = DateTime.UtcNow
            });
            for (int i = 1; i <= 2; i++)
            {
                context.CommandsCommentaires.Add(new CommandsCommentaires
                {
                    id_command_commentaire = 100 + i,
                    id_command = 2,
                    id_user = 21,
                    contenu_command_commentaire = $"UComment {i}",
                    created_at = DateTime.UtcNow.AddMinutes(-i),
                    updated_at = DateTime.UtcNow.AddMinutes(-i)
                });
            }
            await context.SaveChangesAsync();

            var commandCommentaireService = new CommandCommentaireService(_mapper, context, _sessionService.Object);

            var list = await commandCommentaireService.GetCommandsCommentairesByUserId(21, 10, 0);
            var count = await commandCommentaireService.GetCommandsCommentairesCountByUserId(21);

            Assert.NotNull(list);
            Assert.Equal(2, list.Count());
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task GetById_ShouldReturnSingleCommentaire()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Users.Add(new Users
            {
                id_user = 31,
                email_user = "getbyid@test.com",
                nom_user = "Get",
                prenom_user = "One",
                mdp_user = "hashedpwd",
                role_user = UserRole.User
            });
            context.Commands.Add(new Commands
            {
                id_command = 3,
                prix_command = 70,
                url_command = "https://ex.com/c/3",
                status_command = "pending",
                date_command = DateTime.UtcNow
            });
            context.CommandsCommentaires.Add(new CommandsCommentaires
            {
                id_command_commentaire = 200,
                id_command = 3,
                id_user = 31,
                contenu_command_commentaire = "Hello",
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var commandCommentaireService = new CommandCommentaireService(_mapper, context, _sessionService.Object);

            var result = await commandCommentaireService.GetCommandsCommentaireById(200);

            Assert.NotNull(result);
            Assert.Equal(200, result.id_command_commentaire);
            Assert.Equal(3, result.id_command);
            Assert.Equal(31, result.id_user);
            Assert.Equal("Hello", result.contenu_command_commentaire);
        }

        [Fact]
        public async Task Create_ShouldCreateCommentaire_WhenValid()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Users.Add(new Users
            {
                id_user = 41,
                email_user = "create@test.com",
                nom_user = "A",
                prenom_user = "B",
                mdp_user = "hashedpwd",
                role_user = UserRole.User
            });
            context.Commands.Add(new Commands
            {
                id_command = 4,
                prix_command = 12,
                url_command = "https://ex.com/c/4",
                status_command = "pending",
                date_command = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var commandCommentaireService = new CommandCommentaireService(_mapper, context, _sessionService.Object);

            var dto = new CreateCommandCommentaireDto
            {
                id_command = 4,
                id_user = 41,
                contenu_command_commentaire = "New comment"
            };

            var created = await commandCommentaireService.CreateCommentaire(dto);

            Assert.NotNull(created);
            Assert.Equal(4, created.id_command);
            Assert.Equal(41, created.id_user);
            Assert.Equal("New comment", created.contenu_command_commentaire);

            var inDb = await context.CommandsCommentaires.FirstOrDefaultAsync(c => c.id_command == 4 && c.id_user == 41);
            Assert.NotNull(inDb);
        }

        [Fact]
        public async Task Update_ShouldUpdate_WhenAuthorized()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Users.Add(new Users
            {
                id_user = 51,
                email_user = "u@test.com",
                nom_user = "U",
                prenom_user = "S",
                mdp_user = "hashedpwd",
                role_user = UserRole.User
            });
            context.Commands.Add(new Commands
            {
                id_command = 5,
                prix_command = 33,
                url_command = "https://ex.com/c/5",
                status_command = "pending",
                date_command = DateTime.UtcNow
            });
            context.CommandsCommentaires.Add(new CommandsCommentaires
            {
                id_command_commentaire = 300,
                id_command = 5,
                id_user = 51,
                contenu_command_commentaire = "Old",
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            // authorize as owner
            _sessionService.Setup(s => s.GetClientId()).Returns(51);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);

            var commandCommentaireService = new CommandCommentaireService(_mapper, context, _sessionService.Object);

            var updated = await commandCommentaireService.UpdateCommentaire(300, new UpdateCommandCommentaireDto
            {
                contenu_command_commentaire = "Updated"
            });

            Assert.NotNull(updated);
            Assert.Equal("Updated", updated.contenu_command_commentaire);

            var inDb = await context.CommandsCommentaires.FindAsync(300);
            Assert.NotNull(inDb);
            Assert.Equal("Updated", inDb!.contenu_command_commentaire);
        }

        [Fact]
        public async Task Delete_ShouldDelete_WhenAuthorized()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Users.Add(new Users
            {
                id_user = 61,
                email_user = "d@test.com",
                nom_user = "D",
                prenom_user = "E",
                mdp_user = "hashedpwd",
                role_user = UserRole.User
            });
            context.Commands.Add(new Commands
            {
                id_command = 6,
                prix_command = 44,
                url_command = "https://ex.com/c/6",
                status_command = "pending",
                date_command = DateTime.UtcNow
            });
            context.CommandsCommentaires.Add(new CommandsCommentaires
            {
                id_command_commentaire = 400,
                id_command = 6,
                id_user = 61,
                contenu_command_commentaire = "ToDelete",
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            // authorize as owner
            _sessionService.Setup(s => s.GetClientId()).Returns(61);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);

            var commandCommentaireService = new CommandCommentaireService(_mapper, context, _sessionService.Object);

            await commandCommentaireService.DeleteCommentaire(400);

            var inDb = await context.CommandsCommentaires.FindAsync(400);
            Assert.Null(inDb);
        }
    }
}