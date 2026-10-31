using Moq;
using Xunit;
using Microsoft.EntityFrameworkCore;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.EquipementCommentService;
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class EquipementCommentServiceTests : TestBase
    {
        private readonly Mock<ISessionService> _sessionService = new();

        private EquipementCommentService CreateService(ApplicationDbContext context) =>
            new(_mapper, context, _sessionService.Object);

        private static Equipements BuildEquipement(string tag = "equipement") => new()
        {
            reference_name_equipement = tag,
            friendly_name_equipement = tag
        };

        private static Users BuildUser(string email = "user@example.com") => new()
        {
            name_user = "Doe",
            firstname_user = "John",
            email_user = email,
        };

        private async Task<(Equipements parent, Users user)> SeedParentAndUser(ApplicationDbContext context)
        {
            var parent = BuildEquipement();
            var user = BuildUser();
            context.Equipements.Add(parent);
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return (parent, user);
        }

        private static async Task<EquipementsComments> SeedComment(ApplicationDbContext context, int parentId, int? userId, string content = "hello")
        {
            var comment = new EquipementsComments
            {
                id_equipement = parentId,
                id_user = userId,
                content_equipement_comment = content
            };
            context.EquipementsComments.Add(comment);
            await context.SaveChangesAsync();
            return comment;
        }

        private void SetClient(int id, UserRole role)
        {
            _sessionService.Setup(s => s.GetClientId()).Returns(id);
            _sessionService.Setup(s => s.GetClientRole()).Returns(role);
        }

        // --- GetEquipementsCommentsByEquipementId ---

        [Fact]
        public async Task GetEquipementsCommentsByEquipementId_ShouldReturnOnlyCommentsOfParent()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var other = BuildEquipement("other");
            context.Equipements.Add(other);
            await context.SaveChangesAsync();
            await SeedComment(context, parent.id_equipement, user.id_user, "a");
            await SeedComment(context, parent.id_equipement, user.id_user, "b");
            await SeedComment(context, other.id_equipement, user.id_user, "c");
            var service = CreateService(context);

            var result = await service.GetEquipementsCommentsByEquipementId(parent.id_equipement);

            Assert.Equal(2, result.data.Count());
            Assert.All(result.data, c => Assert.Equal(parent.id_equipement, c.id_equipement));
        }

        [Fact]
        public async Task GetEquipementsCommentsByEquipementId_ShouldExpandParentAndUser_WhenRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            await SeedComment(context, parent.id_equipement, user.id_user);
            var service = CreateService(context);

            var result = await service.GetEquipementsCommentsByEquipementId(parent.id_equipement, expand: ["equipement", "user"]);

            var comment = Assert.Single(result.data);
            Assert.NotNull(comment.equipement);
            Assert.NotNull(comment.user);
        }

        [Fact]
        public async Task GetEquipementsCommentsByEquipementId_ShouldNotExpand_WhenNotRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            await SeedComment(context, parent.id_equipement, user.id_user);
            // fresh context: entities tracked by the seeding context would be fixed up into the navigation properties
            using var queryContext = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(queryContext);

            var result = await service.GetEquipementsCommentsByEquipementId(parent.id_equipement);

            var comment = Assert.Single(result.data);
            Assert.Null(comment.equipement);
            Assert.Null(comment.user);
        }

        [Fact]
        public async Task GetEquipementsCommentsByEquipementId_ShouldThrowKeyNotFoundException_WhenParentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetEquipementsCommentsByEquipementId(999));
        }

        // --- GetEquipementsCommentsByUserId ---

        [Fact]
        public async Task GetEquipementsCommentsByUserId_ShouldReturnOnlyCommentsOfUser()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var otherUser = BuildUser("other@example.com");
            context.Users.Add(otherUser);
            await context.SaveChangesAsync();
            await SeedComment(context, parent.id_equipement, user.id_user, "mine");
            await SeedComment(context, parent.id_equipement, otherUser.id_user, "theirs");
            var service = CreateService(context);

            var result = await service.GetEquipementsCommentsByUserId(user.id_user);

            var comment = Assert.Single(result.data);
            Assert.Equal(user.id_user, comment.id_user);
        }

        [Fact]
        public async Task GetEquipementsCommentsByUserId_ShouldExpandParentAndUser_WhenRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            await SeedComment(context, parent.id_equipement, user.id_user);
            var service = CreateService(context);

            var result = await service.GetEquipementsCommentsByUserId(user.id_user, expand: ["equipement", "user"]);

            var comment = Assert.Single(result.data);
            Assert.NotNull(comment.equipement);
            Assert.NotNull(comment.user);
        }

        [Fact]
        public async Task GetEquipementsCommentsByUserId_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetEquipementsCommentsByUserId(999));
        }

        // --- GetEquipementCommentById ---

        [Fact]
        public async Task GetEquipementCommentById_ShouldReturnComment_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_equipement, user.id_user);
            var service = CreateService(context);

            var result = await service.GetEquipementCommentById(comment.id_equipement_comment);

            Assert.Equal(comment.id_equipement_comment, result.id_equipement_comment);
            Assert.Equal("hello", result.content_equipement_comment);
        }

        [Fact]
        public async Task GetEquipementCommentById_ShouldExpandParentAndUser_WhenRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_equipement, user.id_user);
            var service = CreateService(context);

            var result = await service.GetEquipementCommentById(comment.id_equipement_comment, expand: ["equipement", "user"]);

            Assert.NotNull(result.equipement);
            Assert.NotNull(result.user);
        }

        [Fact]
        public async Task GetEquipementCommentById_ShouldThrowKeyNotFoundException_WhenParentIdDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_equipement, user.id_user);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetEquipementCommentById(comment.id_equipement_comment, null, parent.id_equipement + 1));
        }

        [Fact]
        public async Task GetEquipementCommentById_ShouldThrowKeyNotFoundException_WhenUserIdDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_equipement, user.id_user);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetEquipementCommentById(comment.id_equipement_comment, user.id_user + 1));
        }

        [Fact]
        public async Task GetEquipementCommentById_ShouldReturnComment_WhenFiltersMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_equipement, user.id_user);
            var service = CreateService(context);

            var result = await service.GetEquipementCommentById(comment.id_equipement_comment, user.id_user, parent.id_equipement);

            Assert.Equal(comment.id_equipement_comment, result.id_equipement_comment);
        }

        [Fact]
        public async Task GetEquipementCommentById_ShouldThrowKeyNotFoundException_WhenCommentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetEquipementCommentById(999));
        }

        // --- CreateComment ---

        [Fact]
        public async Task CreateComment_ShouldPersistComment()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var service = CreateService(context);

            var result = await service.CreateComment(new CreateEquipementCommentDto { id_equipement = parent.id_equipement, id_user = user.id_user, content_equipement_comment = "new" });

            Assert.Equal("new", result.content_equipement_comment);
            Assert.Equal(user.id_user, result.id_user);
            Assert.Equal(1, await context.EquipementsComments.CountAsync());
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
                service.CreateComment(new CreateEquipementCommentDto { id_equipement = 999, id_user = user.id_user, content_equipement_comment = "x" }));
        }

        [Fact]
        public async Task CreateComment_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var parent = BuildEquipement();
            context.Equipements.Add(parent);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.CreateComment(new CreateEquipementCommentDto { id_equipement = parent.id_equipement, id_user = 999, content_equipement_comment = "x" }));
        }

        // --- UpdateComment ---

        [Fact]
        public async Task UpdateComment_ShouldUpdateContent_WhenClientIsAuthor()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_equipement, user.id_user);
            SetClient(user.id_user, UserRole.User);
            var service = CreateService(context);

            var result = await service.UpdateComment(comment.id_equipement_comment, new UpdateEquipementCommentDto { content_equipement_comment = "edited" });

            Assert.Equal("edited", result.content_equipement_comment);
        }

        [Fact]
        public async Task UpdateComment_ShouldKeepContent_WhenDtoContentIsNull()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_equipement, user.id_user);
            SetClient(user.id_user, UserRole.User);
            var service = CreateService(context);

            var result = await service.UpdateComment(comment.id_equipement_comment, new UpdateEquipementCommentDto());

            Assert.Equal("hello", result.content_equipement_comment);
        }

        [Fact]
        public async Task UpdateComment_ShouldAllowModeratorToEditOthersComment()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_equipement, user.id_user);
            SetClient(user.id_user + 100, UserRole.Moderator);
            var service = CreateService(context);

            var result = await service.UpdateComment(comment.id_equipement_comment, new UpdateEquipementCommentDto { content_equipement_comment = "moderated" });

            Assert.Equal("moderated", result.content_equipement_comment);
        }

        [Fact]
        public async Task UpdateComment_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAuthorNorModerator()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_equipement, user.id_user);
            SetClient(user.id_user + 100, UserRole.User);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.UpdateComment(comment.id_equipement_comment, new UpdateEquipementCommentDto { content_equipement_comment = "nope" }));
        }

        [Fact]
        public async Task UpdateComment_ShouldThrowKeyNotFoundException_WhenCommentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateComment(999, new UpdateEquipementCommentDto()));
        }

        [Fact]
        public async Task UpdateComment_ShouldThrowKeyNotFoundException_WhenParentOrUserFilterDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_equipement, user.id_user);
            SetClient(user.id_user, UserRole.Admin);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.UpdateComment(comment.id_equipement_comment, new UpdateEquipementCommentDto(), null, parent.id_equipement + 1));
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.UpdateComment(comment.id_equipement_comment, new UpdateEquipementCommentDto(), user.id_user + 1));
        }

        // --- DeleteComment ---

        [Fact]
        public async Task DeleteComment_ShouldRemoveComment_WhenClientIsAuthor()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_equipement, user.id_user);
            SetClient(user.id_user, UserRole.User);
            var service = CreateService(context);

            await service.DeleteComment(comment.id_equipement_comment);

            Assert.Equal(0, await context.EquipementsComments.CountAsync());
        }

        [Fact]
        public async Task DeleteComment_ShouldAllowModeratorToDeleteOthersComment()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_equipement, user.id_user);
            SetClient(user.id_user + 100, UserRole.Moderator);
            var service = CreateService(context);

            await service.DeleteComment(comment.id_equipement_comment, user.id_user, parent.id_equipement);

            Assert.Equal(0, await context.EquipementsComments.CountAsync());
        }

        [Fact]
        public async Task DeleteComment_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAuthorNorModerator()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_equipement, user.id_user);
            SetClient(user.id_user + 100, UserRole.User);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteComment(comment.id_equipement_comment));
            Assert.Equal(1, await context.EquipementsComments.CountAsync());
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
            var comment = await SeedComment(context, parent.id_equipement, user.id_user);
            SetClient(user.id_user, UserRole.Admin);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteComment(comment.id_equipement_comment, null, parent.id_equipement + 1));
            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteComment(comment.id_equipement_comment, user.id_user + 1));
        }
    }
}
