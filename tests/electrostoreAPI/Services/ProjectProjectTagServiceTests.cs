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
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Services.ProjectProjectTagService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class ProjectProjectTagServiceTests : TestBase
    {
        private readonly Mock<ISessionService> _sessionService = new();

        public ProjectProjectTagServiceTests()
        {
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Admin);
        }

        private ProjectProjectTagService CreateService(ApplicationDbContext context) =>
            new(_mapper, context, _sessionService.Object);

        private static Projects BuildProject(string name = "project") => new() { name_project = name };

        private static ProjectTags BuildProjectTag(string name = "project_tag") => new() { name_project_tag = name };

        // --- GetProjectsProjectTagsByProjectId ---

        [Fact]
        public async Task GetProjectsProjectTagsByProjectId_ShouldReturnTagsLinkedToProject()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var project = BuildProject();
            var tag = BuildProjectTag();
            context.Projects.Add(project);
            context.ProjectTags.Add(tag);
            await context.SaveChangesAsync();
            context.ProjectsProjectTags.Add(new ProjectsProjectTags { id_project = project.id_project, id_project_tag = tag.id_project_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetProjectsProjectTagsByProjectId(project.id_project);

            var single = Assert.Single(result.data);
            Assert.Equal(tag.id_project_tag, single.id_project_tag);
        }

        [Fact]
        public async Task GetProjectsProjectTagsByProjectId_ShouldThrowKeyNotFoundException_WhenProjectDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetProjectsProjectTagsByProjectId(999));
        }

        // --- GetProjectsProjectTagsByprojectTagId ---

        [Fact]
        public async Task GetProjectsProjectTagsByprojectTagId_ShouldReturnProjectsLinkedToTag()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var project = BuildProject();
            var tag = BuildProjectTag();
            context.Projects.Add(project);
            context.ProjectTags.Add(tag);
            await context.SaveChangesAsync();
            context.ProjectsProjectTags.Add(new ProjectsProjectTags { id_project = project.id_project, id_project_tag = tag.id_project_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetProjectsProjectTagsByprojectTagId(tag.id_project_tag);

            var single = Assert.Single(result.data);
            Assert.Equal(project.id_project, single.id_project);
        }

        [Fact]
        public async Task GetProjectsProjectTagsByprojectTagId_ShouldThrowKeyNotFoundException_WhenTagDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetProjectsProjectTagsByprojectTagId(999));
        }

        // --- GetProjectProjectTagById ---

        [Fact]
        public async Task GetProjectProjectTagById_ShouldReturnProjectProjectTag_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var project = BuildProject();
            var tag = BuildProjectTag();
            context.Projects.Add(project);
            context.ProjectTags.Add(tag);
            await context.SaveChangesAsync();
            context.ProjectsProjectTags.Add(new ProjectsProjectTags { id_project = project.id_project, id_project_tag = tag.id_project_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetProjectProjectTagById(project.id_project, tag.id_project_tag);

            Assert.Equal(project.id_project, result.id_project);
            Assert.Equal(tag.id_project_tag, result.id_project_tag);
        }

        [Fact]
        public async Task GetProjectProjectTagById_ShouldThrowKeyNotFoundException_WhenLinkDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetProjectProjectTagById(1, 1));
        }

        // --- CreateProjectProjectTag ---

        [Fact]
        public async Task CreateProjectProjectTag_ShouldPersistLink_WhenClientIsAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var project = BuildProject();
            var tag = BuildProjectTag();
            context.Projects.Add(project);
            context.ProjectTags.Add(tag);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateProjectProjectTagDto { id_project = project.id_project, id_project_tag = tag.id_project_tag };

            var result = await service.CreateProjectProjectTag(dto);

            Assert.Equal(project.id_project, result.id_project);
            Assert.Equal(1, await context.ProjectsProjectTags.CountAsync());
        }

        [Fact]
        public async Task CreateProjectProjectTag_ShouldThrowKeyNotFoundException_WhenProjectDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tag = BuildProjectTag();
            context.ProjectTags.Add(tag);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateProjectProjectTagDto { id_project = 999, id_project_tag = tag.id_project_tag };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateProjectProjectTag(dto));
        }

        [Fact]
        public async Task CreateProjectProjectTag_ShouldThrowKeyNotFoundException_WhenTagDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var project = BuildProject();
            context.Projects.Add(project);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateProjectProjectTagDto { id_project = project.id_project, id_project_tag = 999 };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateProjectProjectTag(dto));
        }

        [Fact]
        public async Task CreateProjectProjectTag_ShouldThrowInvalidOperationException_WhenLinkAlreadyExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var project = BuildProject();
            var tag = BuildProjectTag();
            context.Projects.Add(project);
            context.ProjectTags.Add(tag);
            await context.SaveChangesAsync();
            context.ProjectsProjectTags.Add(new ProjectsProjectTags { id_project = project.id_project, id_project_tag = tag.id_project_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateProjectProjectTagDto { id_project = project.id_project, id_project_tag = tag.id_project_tag };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateProjectProjectTag(dto));
        }

        [Fact]
        public async Task CreateProjectProjectTag_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);
            var dto = new CreateProjectProjectTagDto { id_project = 1, id_project_tag = 1 };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateProjectProjectTag(dto));
        }

        // --- DeleteProjectProjectTag ---

        [Fact]
        public async Task DeleteProjectProjectTag_ShouldRemoveLink_WhenClientIsAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var project = BuildProject();
            var tag = BuildProjectTag();
            context.Projects.Add(project);
            context.ProjectTags.Add(tag);
            await context.SaveChangesAsync();
            context.ProjectsProjectTags.Add(new ProjectsProjectTags { id_project = project.id_project, id_project_tag = tag.id_project_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.DeleteProjectProjectTag(project.id_project, tag.id_project_tag);

            Assert.Equal(0, await context.ProjectsProjectTags.CountAsync());
        }

        [Fact]
        public async Task DeleteProjectProjectTag_ShouldThrowKeyNotFoundException_WhenLinkDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteProjectProjectTag(1, 1));
        }

        [Fact]
        public async Task DeleteProjectProjectTag_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteProjectProjectTag(1, 1));
        }

        // --- expand / bulk operations ---

        private async Task<(int parentId, int tag1Id, int tag2Id)> SeedForBulk(ApplicationDbContext context)
        {
            var parent = BuildProject();
            context.Projects.Add(parent);
            await context.SaveChangesAsync();
            var tag1 = BuildProjectTag("bulk-1");
            var tag2 = BuildProjectTag("bulk-2");
            context.ProjectTags.AddRange(tag1, tag2);
            await context.SaveChangesAsync();
            return (parent.id_project, tag1.id_project_tag, tag2.id_project_tag);
        }

        [Fact]
        public async Task GetProjectsProjectTagsByProjectId_ShouldExpandParentAndTag_WhenRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parentId, tagId, _) = await SeedForBulk(context);
            context.ProjectsProjectTags.Add(new ProjectsProjectTags { id_project = parentId, id_project_tag = tagId });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetProjectsProjectTagsByProjectId(parentId, expand: ["project", "project_tag"]);

            var link = Assert.Single(result.data);
            Assert.NotNull(link.project);
            Assert.NotNull(link.project_tag);
        }

        [Fact]
        public async Task GetProjectsProjectTagsByprojectTagId_ShouldExpandParentAndTag_WhenRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parentId, tagId, _) = await SeedForBulk(context);
            context.ProjectsProjectTags.Add(new ProjectsProjectTags { id_project = parentId, id_project_tag = tagId });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetProjectsProjectTagsByprojectTagId(tagId, expand: ["project", "project_tag"]);

            var link = Assert.Single(result.data);
            Assert.NotNull(link.project);
            Assert.NotNull(link.project_tag);
        }

        [Fact]
        public async Task GetProjectProjectTagById_ShouldExpandParentAndTag_WhenRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parentId, tagId, _) = await SeedForBulk(context);
            context.ProjectsProjectTags.Add(new ProjectsProjectTags { id_project = parentId, id_project_tag = tagId });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetProjectProjectTagById(parentId, tagId, ["project", "project_tag"]);

            Assert.NotNull(result.project);
            Assert.NotNull(result.project_tag);
        }

        [Fact]
        public async Task CreateBulkProjectProjectTag_ShouldReturnValidAndErrorEntries()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parentId, tag1Id, _) = await SeedForBulk(context);
            var service = CreateService(context);

            var result = await service.CreateBulkProjectProjectTag(
            [
                new CreateProjectProjectTagDto { id_project = parentId, id_project_tag = tag1Id },
                new CreateProjectProjectTagDto { id_project = 9999, id_project_tag = tag1Id },
                new CreateProjectProjectTagDto { id_project = parentId, id_project_tag = tag1Id }
            ]);

            Assert.Single(result.valide);
            Assert.Equal(2, result.error.Count());
            Assert.Equal(1, await context.ProjectsProjectTags.CountAsync());
        }

        [Fact]
        public async Task CreateBulkProjectProjectTag_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateBulkProjectProjectTag([]));
        }

        [Fact]
        public async Task DeleteBulkProjectProjectTag_ShouldReturnValidAndErrorEntries()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parentId, tag1Id, tag2Id) = await SeedForBulk(context);
            context.ProjectsProjectTags.Add(new ProjectsProjectTags { id_project = parentId, id_project_tag = tag1Id });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.DeleteBulkProjectProjectTag(
            [
                new CreateProjectProjectTagDto { id_project = parentId, id_project_tag = tag1Id },
                new CreateProjectProjectTagDto { id_project = parentId, id_project_tag = tag2Id }
            ]);

            var deleted = Assert.Single(result.valide);
            Assert.Equal(tag1Id, deleted.id_project_tag);
            Assert.Single(result.error);
            Assert.Equal(0, await context.ProjectsProjectTags.CountAsync());
        }

        [Fact]
        public async Task DeleteBulkProjectProjectTag_ShouldThrowUnauthorizedAccessException_WhenClientIsNotAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteBulkProjectProjectTag([]));
        }
    }
}
