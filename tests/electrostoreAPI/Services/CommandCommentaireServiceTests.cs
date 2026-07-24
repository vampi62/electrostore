using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.CommandCommentaireService;
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class CommandCommentaireServiceTests : TestBase
    {
        private readonly Mock<ISessionService> _sessionService;

        public CommandCommentaireServiceTests()
        {
            _sessionService = new Mock<ISessionService>();
        }

        private CommandCommentaireService CreateService(ApplicationDbContext context)
        {
            return new CommandCommentaireService(_mapper, context, _sessionService.Object);
        }

        private void SetClient(int id, UserRole role)
        {
            _sessionService.Setup(s => s.GetClientId()).Returns(id);
            _sessionService.Setup(s => s.GetClientRole()).Returns(role);
        }

        private static Commands BuildCommand(int id)
        {
            return new Commands
            {
                id_command = id,
                url_command = "https://example.com/order",
                tracking_number = "TRACK-" + id,
                date_command = DateTime.UtcNow
            };
        }

        private static Users BuildUser(int id)
        {
            return new Users
            {
                id_user = id,
                nom_user = "Nom",
                prenom_user = "Prenom",
                email_user = "user" + id + "@test.com",
                mdp_user = "hashedpassword"
            };
        }

        private static CommandsCommentaires BuildCommentaire(int id, int commandId, int? userId, string content = "Comment")
        {
            return new CommandsCommentaires
            {
                id_command_commentaire = id,
                id_command = commandId,
                id_user = userId,
                contenu_command_commentaire = content
            };
        }

        // --- GetCommandsCommentairesByCommandId ---

        [Fact]
        public async Task GetCommandsCommentairesByCommandId_ShouldThrowKeyNotFoundException_WhenCommandNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetCommandsCommentairesByCommandId(1);
            });
        }

        [Fact]
        public async Task GetCommandsCommentairesByCommandId_ShouldReturnOnlyForGivenCommand()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Commands.Add(BuildCommand(2));
            context.Users.Add(BuildUser(1));
            context.CommandsCommentaires.Add(BuildCommentaire(1, 1, 1));
            context.CommandsCommentaires.Add(BuildCommentaire(2, 1, 1));
            context.CommandsCommentaires.Add(BuildCommentaire(3, 2, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetCommandsCommentairesByCommandId(1);
            // Assert
            Assert.Equal(2, result.data.Count());
            Assert.Equal(2, result.pagination.total);
        }

        // --- GetCommandsCommentairesByUserId ---

        [Fact]
        public async Task GetCommandsCommentairesByUserId_ShouldThrowKeyNotFoundException_WhenUserNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetCommandsCommentairesByUserId(1);
            });
        }

        [Fact]
        public async Task GetCommandsCommentairesByUserId_ShouldReturnOnlyForGivenUser()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Users.Add(BuildUser(1));
            context.Users.Add(BuildUser(2));
            context.CommandsCommentaires.Add(BuildCommentaire(1, 1, 1));
            context.CommandsCommentaires.Add(BuildCommentaire(2, 1, 2));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetCommandsCommentairesByUserId(1);
            // Assert
            var commentaire = Assert.Single(result.data);
            Assert.Equal(1, commentaire.id_user);
        }

        // --- GetCommandsCommentaireById ---

        [Fact]
        public async Task GetCommandsCommentaireById_ShouldReturnCommentaire_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Users.Add(BuildUser(1));
            context.CommandsCommentaires.Add(BuildCommentaire(1, 1, 1, "Hello"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetCommandsCommentaireById(1);
            // Assert
            Assert.Equal("Hello", result.contenu_command_commentaire);
        }

        [Fact]
        public async Task GetCommandsCommentaireById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetCommandsCommentaireById(999);
            });
        }

        // --- CreateCommentaire ---

        [Fact]
        public async Task CreateCommentaire_ShouldThrowKeyNotFoundException_WhenCommandNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateCommandCommentaireDto { id_command = 999, id_user = 1, contenu_command_commentaire = "Hello" };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreateCommentaire(dto);
            });
        }

        [Fact]
        public async Task CreateCommentaire_ShouldThrowKeyNotFoundException_WhenUserNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateCommandCommentaireDto { id_command = 1, id_user = 999, contenu_command_commentaire = "Hello" };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreateCommentaire(dto);
            });
        }

        [Fact]
        public async Task CreateCommentaire_ShouldCreateCommentaire_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Users.Add(BuildUser(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateCommandCommentaireDto { id_command = 1, id_user = 1, contenu_command_commentaire = "Hello" };
            // Act
            var result = await service.CreateCommentaire(dto);
            // Assert
            Assert.Equal("Hello", result.contenu_command_commentaire);
            Assert.Equal(1, await context.CommandsCommentaires.CountAsync());
        }

        // --- UpdateCommentaire ---

        [Fact]
        public async Task UpdateCommentaire_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateCommentaire(999, new UpdateCommandCommentaireDto());
            });
        }

        [Fact]
        public async Task UpdateCommentaire_ShouldThrowUnauthorizedAccessException_WhenNotOwnerNorModerator()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Users.Add(BuildUser(1));
            context.CommandsCommentaires.Add(BuildCommentaire(1, 1, 1));
            await context.SaveChangesAsync();
            SetClient(2, UserRole.User);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.UpdateCommentaire(1, new UpdateCommandCommentaireDto { contenu_command_commentaire = "Edited" });
            });
        }

        [Fact]
        public async Task UpdateCommentaire_ShouldUpdateContent_WhenOwner()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Users.Add(BuildUser(1));
            context.CommandsCommentaires.Add(BuildCommentaire(1, 1, 1, "Old"));
            await context.SaveChangesAsync();
            SetClient(1, UserRole.User);
            var service = CreateService(context);
            // Act
            var result = await service.UpdateCommentaire(1, new UpdateCommandCommentaireDto { contenu_command_commentaire = "New" });
            // Assert
            Assert.Equal("New", result.contenu_command_commentaire);
        }

        [Fact]
        public async Task UpdateCommentaire_ShouldUpdateContent_WhenModerator()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Users.Add(BuildUser(1));
            context.CommandsCommentaires.Add(BuildCommentaire(1, 1, 1, "Old"));
            await context.SaveChangesAsync();
            SetClient(2, UserRole.Moderator);
            var service = CreateService(context);
            // Act
            var result = await service.UpdateCommentaire(1, new UpdateCommandCommentaireDto { contenu_command_commentaire = "New" });
            // Assert
            Assert.Equal("New", result.contenu_command_commentaire);
        }

        // --- DeleteCommentaire ---

        [Fact]
        public async Task DeleteCommentaire_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeleteCommentaire(999);
            });
        }

        [Fact]
        public async Task DeleteCommentaire_ShouldThrowUnauthorizedAccessException_WhenNotOwnerNorModerator()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Users.Add(BuildUser(1));
            context.CommandsCommentaires.Add(BuildCommentaire(1, 1, 1));
            await context.SaveChangesAsync();
            SetClient(2, UserRole.User);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.DeleteCommentaire(1);
            });
        }

        [Fact]
        public async Task DeleteCommentaire_ShouldDelete_WhenOwner()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Commands.Add(BuildCommand(1));
            context.Users.Add(BuildUser(1));
            context.CommandsCommentaires.Add(BuildCommentaire(1, 1, 1));
            await context.SaveChangesAsync();
            SetClient(1, UserRole.User);
            var service = CreateService(context);
            // Act
            await service.DeleteCommentaire(1);
            // Assert
            Assert.Equal(0, await context.CommandsCommentaires.CountAsync());
        }
    }
}
