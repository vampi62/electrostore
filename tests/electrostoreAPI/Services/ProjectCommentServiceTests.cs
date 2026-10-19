using Moq;
using Xunit;
using Microsoft.EntityFrameworkCore;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.ProjectCommentService;
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class ProjectCommentServiceTests : TestBase
    {
        private readonly Mock<ISessionService> _sessionService = new();

        private ProjectCommentService CreateService(ApplicationDbContext context) =>
            new(_mapper, context, _sessionService.Object);

        private static Projects BuildProject(string tag = "project") => new()
        {
            name_project = tag
        };

        private static Users BuildUser(string email = "user@example.com") => new()
        {
            name_user = "Doe",
            firstname_user = "John",
            email_user = email,
            password_user = "hash"
        };

        private async Task<(Projects parent, Users user)> SeedParentAndUser(ApplicationDbContext context)
        {
            var parent = BuildProject();
            var user = BuildUser();
            context.Projects.Add(parent);
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return (parent, user);
        }

        private static async Task<ProjectsComments> SeedComment(ApplicationDbContext context, int parentId, int? userId, string content = "hello")
        {
            var comment = new ProjectsComments
            {
                id_project = parentId,
                id_user = userId,
                content_project_comment = content
            };
            context.ProjectsComments.Add(comment);
            await context.SaveChangesAsync();
            return comment;
        }

        private void SetClient(int id, UserRole role)
        {
            _sessionService.Setup(s => s.GetClientId()).Returns(id);
            _sessionService.Setup(s => s.GetClientRole()).Returns(role);
        }

        // --- GetProjectCommentsByProjectId ---

        [Fact]
        public async Task GetProjectCommentsByProjectId_ShouldReturnOnlyCommentsOfParent()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var other = BuildProject("other");
            context.Projects.Add(other);
            await context.SaveChangesAsync();
            await SeedComment(context, parent.id_project, user.id_user, "a");
            await SeedComment(context, parent.id_project, user.id_user, "b");
            await SeedComment(context, other.id_project, user.id_user, "c");
            var service = CreateService(context);

            var result = await service.GetProjectCommentsByProjectId(parent.id_project);

            Assert.Equal(2, result.data.Count());
            Assert.All(result.data, c => Assert.Equal(parent.id_project, c.id_project));
        }

        [Fact]
        public async Task GetProjectCommentsByProjectId_ShouldExpandParentAndUser_WhenRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            await SeedComment(context, parent.id_project, user.id_user);
            var service = CreateService(context);

            var result = await service.GetProjectCommentsByProjectId(parent.id_project, expand: ["project", "user"]);

            var comment = Assert.Single(result.data);
            Assert.NotNull(comment.project);
            Assert.NotNull(comment.user);
        }

        [Fact]
        public async Task GetProjectCommentsByProjectId_ShouldNotExpand_WhenNotRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            await SeedComment(context, parent.id_project, user.id_user);
            // fresh context: entities tracked by the seeding context would be fixed up into the navigation properties
            using var queryContext = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(queryContext);

            var result = await service.GetProjectCommentsByProjectId(parent.id_project);

            var comment = Assert.Single(result.data);
            Assert.Null(comment.project);
            Assert.Null(comment.user);
        }

        [Fact]
        public async Task GetProjectCommentsByProjectId_ShouldThrowKeyNotFoundException_WhenParentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetProjectCommentsByProjectId(999));
        }

        // --- GetProjectCommentsByUserId ---

        [Fact]
        public async Task GetProjectCommentsByUserId_ShouldReturnOnlyCommentsOfUser()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var otherUser = BuildUser("other@example.com");
            context.Users.Add(otherUser);
            await context.SaveChangesAsync();
            await SeedComment(context, parent.id_project, user.id_user, "mine");
            await SeedComment(context, parent.id_project, otherUser.id_user, "theirs");
            var service = CreateService(context);

            var result = await service.GetProjectCommentsByUserId(user.id_user);

            var comment = Assert.Single(result.data);
            Assert.Equal(user.id_user, comment.id_user);
        }

        [Fact]
        public async Task GetProjectCommentsByUserId_ShouldExpandParentAndUser_WhenRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            await SeedComment(context, parent.id_project, user.id_user);
            var service = CreateService(context);

            var result = await service.GetProjectCommentsByUserId(user.id_user, expand: ["project", "user"]);

            var comment = Assert.Single(result.data);
            Assert.NotNull(comment.project);
            Assert.NotNull(comment.user);
        }

        [Fact]
        public async Task GetProjectCommentsByUserId_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetProjectCommentsByUserId(999));
        }

        // --- GetProjectCommentsById ---

        [Fact]
        public async Task GetProjectCommentsById_ShouldReturnComment_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_project, user.id_user);
            var service = CreateService(context);

            var result = await service.GetProjectCommentsById(comment.id_project_comment);

            Assert.Equal(comment.id_project_comment, result.id_project_comment);
            Assert.Equal("hello", result.content_project_comment);
        }

        [Fact]
        public async Task GetProjectCommentsById_ShouldExpandParentAndUser_WhenRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_project, user.id_user);
            var service = CreateService(context);

            var result = await service.GetProjectCommentsById(comment.id_project_comment, expand: ["project", "user"]);

            Assert.NotNull(result.project);
            Assert.NotNull(result.user);
        }

        [Fact]
        public async Task GetProjectCommentsById_ShouldThrowKeyNotFoundException_WhenParentIdDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_project, user.id_user);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetProjectCommentsById(comment.id_project_comment, null, parent.id_project + 1));
        }

        [Fact]
        public async Task GetProjectCommentsById_ShouldThrowKeyNotFoundException_WhenUserIdDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_project, user.id_user);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetProjectCommentsById(comment.id_project_comment, user.id_user + 1));
        }

        [Fact]
        public async Task GetProjectCommentsById_ShouldReturnComment_WhenFiltersMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_project, user.id_user);
            var service = CreateService(context);

            var result = await service.GetProjectCommentsById(comment.id_project_comment, user.id_user, parent.id_project);

            Assert.Equal(comment.id_project_comment, result.id_project_comment);
        }

        [Fact]
        public async Task GetProjectCommentsById_ShouldThrowKeyNotFoundException_WhenCommentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetProjectCommentsById(999));
        }

        // --- CreateProjectComment ---

        [Fact]
        public async Task CreateProjectComment_ShouldPersistComment()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var service = CreateService(context);

            var result = await service.CreateProjectComment(new CreateProjectCommentDto { id_project = parent.id_project, id_user = user.id_user, content_project_comment = "new" });

            Assert.Equal("new", result.content_project_comment);
            Assert.Equal(user.id_user, result.id_user);
            Assert.Equal(1, await context.ProjectsComments.CountAsync());
        }

        [Fact]
        public async Task CreateProjectComment_ShouldThrowKeyNotFoundException_WhenParentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = BuildUser();
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.CreateProjectComment(new CreateProjectCommentDto { id_project = 999, id_user = user.id_user, content_project_comment = "x" }));
        }

        [Fact]
        public async Task CreateProjectComment_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var parent = BuildProject();
            context.Projects.Add(parent);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.CreateProjectComment(new CreateProjectCommentDto { id_project = parent.id_project, id_user = 999, content_project_comment = "x" }));
        }

        // --- UpdateProjectComment ---

        [Fact]
        public async Task UpdateProjectComment_ShouldUpdateContent_WhenClientIsAuthor()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_project, user.id_user);
            SetClient(user.id_user, UserRole.User);
            var service = CreateService(context);

            var result = await service.UpdateProjectComment(comment.id_project_comment, new UpdateProjectCommentDto { content_project_comment = "edited" });

            Assert.Equal("edited", result.content_project_comment);
        }

        [Fact]
        public async Task UpdateProjectComment_ShouldKeepContent_WhenDtoContentIsNull()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_project, user.id_user);
            SetClient(user.id_user, UserRole.User);
            var service = CreateService(context);

            var result = await service.UpdateProjectComment(comment.id_project_comment, new UpdateProjectCommentDto());

            Assert.Equal("hello", result.content_project_comment);
        }

        [Fact]
        public async Task UpdateProjectComment_ShouldAllowModeratorToEditOthersComment()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_project, user.id_user);
            SetClient(user.id_user + 100, UserRole.Moderator);
            var service = CreateService(context);

            var result = await service.UpdateProjectComment(comment.id_project_comment, new UpdateProjectCommentDto { content_project_comment = "moderated" });

            Assert.Equal("moderated", result.content_project_comment);
        }

        [Fact]
        public async Task UpdateProjectComment_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAuthorNorModerator()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_project, user.id_user);
            SetClient(user.id_user + 100, UserRole.User);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.UpdateProjectComment(comment.id_project_comment, new UpdateProjectCommentDto { content_project_comment = "nope" }));
        }

        [Fact]
        public async Task UpdateProjectComment_ShouldThrowKeyNotFoundException_WhenCommentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateProjectComment(999, new UpdateProjectCommentDto()));
        }

        [Fact]
        public async Task UpdateProjectComment_ShouldThrowKeyNotFoundException_WhenParentOrUserFilterDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_project, user.id_user);
            SetClient(user.id_user, UserRole.Admin);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.UpdateProjectComment(comment.id_project_comment, new UpdateProjectCommentDto(), null, parent.id_project + 1));
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.UpdateProjectComment(comment.id_project_comment, new UpdateProjectCommentDto(), user.id_user + 1));
        }

        // --- DeleteProjectComment ---

        [Fact]
        public async Task DeleteProjectComment_ShouldRemoveComment_WhenClientIsAuthor()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_project, user.id_user);
            SetClient(user.id_user, UserRole.User);
            var service = CreateService(context);

            await service.DeleteProjectComment(comment.id_project_comment);

            Assert.Equal(0, await context.ProjectsComments.CountAsync());
        }

        [Fact]
        public async Task DeleteProjectComment_ShouldAllowModeratorToDeleteOthersComment()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_project, user.id_user);
            SetClient(user.id_user + 100, UserRole.Moderator);
            var service = CreateService(context);

            await service.DeleteProjectComment(comment.id_project_comment, user.id_user, parent.id_project);

            Assert.Equal(0, await context.ProjectsComments.CountAsync());
        }

        [Fact]
        public async Task DeleteProjectComment_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAuthorNorModerator()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_project, user.id_user);
            SetClient(user.id_user + 100, UserRole.User);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteProjectComment(comment.id_project_comment));
            Assert.Equal(1, await context.ProjectsComments.CountAsync());
        }

        [Fact]
        public async Task DeleteProjectComment_ShouldThrowKeyNotFoundException_WhenCommentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteProjectComment(999));
        }

        [Fact]
        public async Task DeleteProjectComment_ShouldThrowKeyNotFoundException_WhenParentOrUserFilterDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, user) = await SeedParentAndUser(context);
            var comment = await SeedComment(context, parent.id_project, user.id_user);
            SetClient(user.id_user, UserRole.Admin);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteProjectComment(comment.id_project_comment, null, parent.id_project + 1));
            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteProjectComment(comment.id_project_comment, user.id_user + 1));
        }
    }
}
