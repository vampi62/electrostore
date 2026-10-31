using Moq;
using Xunit;
using Microsoft.EntityFrameworkCore;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.CommandCommentService;
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class CommandCommentServiceTests : TestBase
    {
        private readonly Mock<ISessionService> _sessionService = new();

        private CommandCommentService CreateService(ApplicationDbContext context) =>
            new(_mapper, context, _sessionService.Object);

        private static Commands BuildCommand(string tag = "command") => new()
        {
            url_command = tag,
            date_command = DateTime.UtcNow
        };

        private static Users BuildUser(string email = "user@example.com") => new()
        {
            name_user = "Doe",
            firstname_user = "John",
            email_user = email,
        };

        private async Task<(Commands parent, Users user)> SeedParentAndUser(ApplicationDbContext context)
        {
            var parent = BuildCommand();
            var user = BuildUser();
            context.Commands.Add(parent);
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return (parent, user);
        }

        private static async Task<CommandsComments> SeedComment(ApplicationDbContext context, int parentId, int? userId, string content = "hello")
        {
            var comment = new CommandsComments
            {
                id_command = parentId,
                id_user = userId,
                content_command_comment = content
            };
            context.CommandsComments.Add(comment);
            await context.SaveChangesAsync();
            return comment;
        }

        private void SetClient(int id, UserRole role)
        {
            _sessionService.Setup(s => s.GetClientId()).Returns(id);
            _sessionService.Setup(s => s.GetClientRole()).Returns(role);
        }

        // --- GetCommandsCommentsByCommandId ---

        [Fact]
        public async Task GetCommandsCommentsByCommandId_ShouldReturnOnlyCommentsOfParent()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var other = BuildCommand("other");
            context.Commands.Add(other);
            await context.SaveChangesAsync();
            await SeedComment(context, parent.id_command, user.id_user, "a");
            await SeedComment(context, parent.id_command, user.id_user, "b");
            await SeedComment(context, other.id_command, user.id_user, "c");
            var service = CreateService(context);

            var result = await service.GetCommandsCommentsByCommandId(parent.id_command);

            Assert.Equal(2, result.data.Count());
            Assert.All(result.data, c => Assert.Equal(parent.id_command, c.id_command));
        }

        [Fact]
        public async Task GetCommandsCommentsByCommandId_ShouldExpandParentAndUser_WhenRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            await SeedComment(context, parent.id_command, user.id_user);
            var service = CreateService(context);

            var result = await service.GetCommandsCommentsByCommandId(parent.id_command, expand: ["command", "user"]);

            var comment = Assert.Single(result.data);
            Assert.NotNull(comment.command);
            Assert.NotNull(comment.user);
        }

        [Fact]
        public async Task GetCommandsCommentsByCommandId_ShouldNotExpand_WhenNotRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            await SeedComment(context, parent.id_command, user.id_user);
            // fresh context: entities tracked by the seeding context would be fixed up into the navigation properties
            using var queryContext = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(queryContext);

            var result = await service.GetCommandsCommentsByCommandId(parent.id_command);

            var comment = Assert.Single(result.data);
            Assert.Null(comment.command);
            Assert.Null(comment.user);
        }

        [Fact]
        public async Task GetCommandsCommentsByCommandId_ShouldThrowKeyNotFoundException_WhenParentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetCommandsCommentsByCommandId(999));
        }

        // --- GetCommandsCommentsByUserId ---

        [Fact]
        public async Task GetCommandsCommentsByUserId_ShouldReturnOnlyCommentsOfUser()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var otherUser = BuildUser("other@example.com");
            context.Users.Add(otherUser);
            await context.SaveChangesAsync();
            await SeedComment(context, parent.id_command, user.id_user, "mine");
            await SeedComment(context, parent.id_command, otherUser.id_user, "theirs");
            var service = CreateService(context);

            var result = await service.GetCommandsCommentsByUserId(user.id_user);

            var comment = Assert.Single(result.data);
            Assert.Equal(user.id_user, comment.id_user);
        }

        [Fact]
        public async Task GetCommandsCommentsByUserId_ShouldExpandParentAndUser_WhenRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            await SeedComment(context, parent.id_command, user.id_user);
            var service = CreateService(context);

            var result = await service.GetCommandsCommentsByUserId(user.id_user, expand: ["command", "user"]);

            var comment = Assert.Single(result.data);
            Assert.NotNull(comment.command);
            Assert.NotNull(comment.user);
        }

        [Fact]
        public async Task GetCommandsCommentsByUserId_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetCommandsCommentsByUserId(999));
        }

        // --- GetCommandsCommentById ---

        [Fact]
        public async Task GetCommandsCommentById_ShouldReturnComment_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_command, user.id_user);
            var service = CreateService(context);

            var result = await service.GetCommandsCommentById(comment.id_command_comment);

            Assert.Equal(comment.id_command_comment, result.id_command_comment);
            Assert.Equal("hello", result.content_command_comment);
        }

        [Fact]
        public async Task GetCommandsCommentById_ShouldExpandParentAndUser_WhenRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_command, user.id_user);
            var service = CreateService(context);

            var result = await service.GetCommandsCommentById(comment.id_command_comment, expand: ["command", "user"]);

            Assert.NotNull(result.command);
            Assert.NotNull(result.user);
        }

        [Fact]
        public async Task GetCommandsCommentById_ShouldThrowKeyNotFoundException_WhenParentIdDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_command, user.id_user);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetCommandsCommentById(comment.id_command_comment, null, parent.id_command + 1));
        }

        [Fact]
        public async Task GetCommandsCommentById_ShouldThrowKeyNotFoundException_WhenUserIdDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_command, user.id_user);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetCommandsCommentById(comment.id_command_comment, user.id_user + 1));
        }

        [Fact]
        public async Task GetCommandsCommentById_ShouldReturnComment_WhenFiltersMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_command, user.id_user);
            var service = CreateService(context);

            var result = await service.GetCommandsCommentById(comment.id_command_comment, user.id_user, parent.id_command);

            Assert.Equal(comment.id_command_comment, result.id_command_comment);
        }

        [Fact]
        public async Task GetCommandsCommentById_ShouldThrowKeyNotFoundException_WhenCommentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetCommandsCommentById(999));
        }

        // --- CreateComment ---

        [Fact]
        public async Task CreateComment_ShouldPersistComment()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var service = CreateService(context);

            var result = await service.CreateComment(new CreateCommandCommentDto { id_command = parent.id_command, id_user = user.id_user, content_command_comment = "new" });

            Assert.Equal("new", result.content_command_comment);
            Assert.Equal(user.id_user, result.id_user);
            Assert.Equal(1, await context.CommandsComments.CountAsync());
        }

        [Fact]
        public async Task CreateComment_ShouldThrowKeyNotFoundException_WhenParentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = BuildUser();
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.CreateComment(new CreateCommandCommentDto { id_command = 999, id_user = user.id_user, content_command_comment = "x" }));
        }

        [Fact]
        public async Task CreateComment_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var parent = BuildCommand();
            context.Commands.Add(parent);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.CreateComment(new CreateCommandCommentDto { id_command = parent.id_command, id_user = 999, content_command_comment = "x" }));
        }

        // --- UpdateComment ---

        [Fact]
        public async Task UpdateComment_ShouldUpdateContent_WhenClientIsAuthor()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_command, user.id_user);
            SetClient(user.id_user, UserRole.User);
            var service = CreateService(context);

            var result = await service.UpdateComment(comment.id_command_comment, new UpdateCommandCommentDto { content_command_comment = "edited" });

            Assert.Equal("edited", result.content_command_comment);
        }

        [Fact]
        public async Task UpdateComment_ShouldKeepContent_WhenDtoContentIsNull()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_command, user.id_user);
            SetClient(user.id_user, UserRole.User);
            var service = CreateService(context);

            var result = await service.UpdateComment(comment.id_command_comment, new UpdateCommandCommentDto());

            Assert.Equal("hello", result.content_command_comment);
        }

        [Fact]
        public async Task UpdateComment_ShouldAllowModeratorToEditOthersComment()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_command, user.id_user);
            SetClient(user.id_user + 100, UserRole.Moderator);
            var service = CreateService(context);

            var result = await service.UpdateComment(comment.id_command_comment, new UpdateCommandCommentDto { content_command_comment = "moderated" });

            Assert.Equal("moderated", result.content_command_comment);
        }

        [Fact]
        public async Task UpdateComment_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAuthorNorModerator()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_command, user.id_user);
            SetClient(user.id_user + 100, UserRole.User);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.UpdateComment(comment.id_command_comment, new UpdateCommandCommentDto { content_command_comment = "nope" }));
        }

        [Fact]
        public async Task UpdateComment_ShouldThrowKeyNotFoundException_WhenCommentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateComment(999, new UpdateCommandCommentDto()));
        }

        [Fact]
        public async Task UpdateComment_ShouldThrowKeyNotFoundException_WhenParentOrUserFilterDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_command, user.id_user);
            SetClient(user.id_user, UserRole.Admin);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.UpdateComment(comment.id_command_comment, new UpdateCommandCommentDto(), null, parent.id_command + 1));
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.UpdateComment(comment.id_command_comment, new UpdateCommandCommentDto(), user.id_user + 1));
        }

        // --- DeleteComment ---

        [Fact]
        public async Task DeleteComment_ShouldRemoveComment_WhenClientIsAuthor()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_command, user.id_user);
            SetClient(user.id_user, UserRole.User);
            var service = CreateService(context);

            await service.DeleteComment(comment.id_command_comment);

            Assert.Equal(0, await context.CommandsComments.CountAsync());
        }

        [Fact]
        public async Task DeleteComment_ShouldAllowModeratorToDeleteOthersComment()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_command, user.id_user);
            SetClient(user.id_user + 100, UserRole.Moderator);
            var service = CreateService(context);

            await service.DeleteComment(comment.id_command_comment, user.id_user, parent.id_command);

            Assert.Equal(0, await context.CommandsComments.CountAsync());
        }

        [Fact]
        public async Task DeleteComment_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAuthorNorModerator()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_command, user.id_user);
            SetClient(user.id_user + 100, UserRole.User);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteComment(comment.id_command_comment));
            Assert.Equal(1, await context.CommandsComments.CountAsync());
        }

        [Fact]
        public async Task DeleteComment_ShouldThrowKeyNotFoundException_WhenCommentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteComment(999));
        }

        [Fact]
        public async Task DeleteComment_ShouldThrowKeyNotFoundException_WhenParentOrUserFilterDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_command, user.id_user);
            SetClient(user.id_user, UserRole.Admin);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteComment(comment.id_command_comment, null, parent.id_command + 1));
            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteComment(comment.id_command_comment, user.id_user + 1));
        }
    }
}
