using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Services.ProjectService;
using ElectrostoreAPI.Services.ProjectStatusService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class ProjectServiceTests : TestBase
    {
        private readonly Mock<IFileService> _fileService = new();
        private readonly Mock<IProjectStatusService> _projectStatusService = new();

        private ProjectService CreateService(ApplicationDbContext context) =>
            new(_mapper, context, _fileService.Object, _projectStatusService.Object);

        private static Projects BuildProject(string name = "project", ProjectStatus status = ProjectStatus.NotStarted) => new()
        {
            name_project = name,
            status_project = status
        };

        private static Items BuildItem(string reference = "item") => new()
        {
            reference_name_item = reference,
            friendly_name_item = reference,
            threshold_min_item = 0
        };

        // --- GetProjects ---

        [Fact]
        public async Task GetProjects_ShouldReturnAllProjects_WhenNoFilterApplied()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Projects.AddRange(BuildProject("a"), BuildProject("b"));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetProjects();

            Assert.Equal(2, result.pagination.total);
            Assert.Equal(2, result.data.Count());
        }

        [Fact]
        public async Task GetProjects_ShouldRestrictToIds_WhenAnyFilterProvided()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var a = BuildProject("a");
            var b = BuildProject("b");
            context.Projects.AddRange(a, b);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetProjects(rsql: [new FilterDto { field = "id_project", search_type = "any", value = b.id_project.ToString() }]);

            var single = Assert.Single(result.data);
            Assert.Equal(b.id_project, single.id_project);
        }

        [Fact]
        public async Task GetProjects_ShouldComputeCountsAndStartEndDates()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var project = BuildProject();
            var item = BuildItem();
            context.Projects.Add(project);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            context.ProjectsStatus.AddRange(
                new ProjectsStatus { id_project = project.id_project, status_project = ProjectStatus.InProgress },
                new ProjectsStatus { id_project = project.id_project, status_project = ProjectStatus.Completed });
            context.ProjectsItems.Add(new ProjectsItems { id_project = project.id_project, id_item = item.id_item, quantity_project_item = 2 });
            context.ProjectsComments.Add(new ProjectsComments { id_project = project.id_project, content_project_comment = "hi" });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetProjects();

            var single = Assert.Single(result.data);
            Assert.NotNull(single.date_start_project);
            Assert.NotNull(single.date_end_project);
            Assert.Equal(2, single.project_status_history_count);
            Assert.Equal(1, single.project_items_count);
            Assert.Equal(1, single.project_comments_count);
            Assert.Equal(0, single.project_documents_count);
            Assert.Equal(0, single.project_tags_count);
        }

        [Fact]
        public async Task GetProjects_ShouldReturnRelatedEntities_WhenExpanded()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var project = BuildProject();
            var item = BuildItem();
            var tag = new ProjectTags { name_project_tag = "tag" };
            context.Projects.Add(project);
            context.Items.Add(item);
            context.ProjectTags.Add(tag);
            await context.SaveChangesAsync();
            context.ProjectsItems.Add(new ProjectsItems { id_project = project.id_project, id_item = item.id_item, quantity_project_item = 1 });
            context.ProjectsProjectTags.Add(new ProjectsProjectTags { id_project = project.id_project, id_project_tag = tag.id_project_tag });
            context.ProjectsComments.Add(new ProjectsComments { id_project = project.id_project, content_project_comment = "hi" });
            context.ProjectsDocuments.Add(new ProjectsDocuments
            {
                id_project = project.id_project,
                url_project_document = "p/doc.pdf",
                name_project_document = "doc",
                type_project_document = "application/pdf",
                size_project_document = 1
            });
            context.ProjectsStatus.Add(new ProjectsStatus { id_project = project.id_project, status_project = ProjectStatus.NotStarted });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetProjects(expand: ["project_comments", "project_documents", "project_items", "project_tags", "project_status_history"]);

            var single = Assert.Single(result.data);
            Assert.Single(single.project_comments!);
            Assert.Single(single.project_documents!);
            Assert.Single(single.project_items!);
            Assert.Single(single.project_tags!);
            Assert.Single(single.project_status_history!);
        }

        // --- GetProjectById ---

        [Fact]
        public async Task GetProjectById_ShouldReturnProject_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var project = BuildProject("mine");
            context.Projects.Add(project);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetProjectById(project.id_project);

            Assert.Equal("mine", result.name_project);
            Assert.Null(result.date_start_project);
            Assert.Null(result.date_end_project);
        }

        [Fact]
        public async Task GetProjectById_ShouldComputeDatesAndExpandRelations()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var project = BuildProject();
            var item = BuildItem();
            var tag = new ProjectTags { name_project_tag = "tag" };
            context.Projects.Add(project);
            context.Items.Add(item);
            context.ProjectTags.Add(tag);
            await context.SaveChangesAsync();
            context.ProjectsStatus.Add(new ProjectsStatus { id_project = project.id_project, status_project = ProjectStatus.InProgress });
            context.ProjectsItems.Add(new ProjectsItems { id_project = project.id_project, id_item = item.id_item, quantity_project_item = 1 });
            context.ProjectsProjectTags.Add(new ProjectsProjectTags { id_project = project.id_project, id_project_tag = tag.id_project_tag });
            context.ProjectsComments.Add(new ProjectsComments { id_project = project.id_project, content_project_comment = "hi" });
            context.ProjectsDocuments.Add(new ProjectsDocuments
            {
                id_project = project.id_project,
                url_project_document = "p/doc.pdf",
                name_project_document = "doc",
                type_project_document = "application/pdf",
                size_project_document = 1
            });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetProjectById(project.id_project, ["project_comments", "project_documents", "project_items", "project_tags", "project_status_history"]);

            Assert.NotNull(result.date_start_project);
            Assert.Null(result.date_end_project);
            Assert.Equal(1, result.project_items_count);
            Assert.Single(result.project_comments!);
            Assert.Single(result.project_documents!);
            Assert.Single(result.project_items!);
            Assert.Single(result.project_tags!);
            Assert.Single(result.project_status_history!);
        }

        [Fact]
        public async Task GetProjectById_ShouldThrowKeyNotFoundException_WhenProjectDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetProjectById(999));
        }

        // --- CreateProject ---

        [Fact]
        public async Task CreateProject_ShouldPersistProject_CreateDirectoryAndInitialStatus()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            var result = await service.CreateProject(new CreateProjectDto { name_project = "new", status_project = ProjectStatus.InProgress });

            Assert.Equal("new", result.name_project);
            Assert.Equal(1, await context.Projects.CountAsync());
            _fileService.Verify(f => f.CreateDirectory(It.Is<string>(p => p.StartsWith("projectDocuments"))), Times.Once);
            _projectStatusService.Verify(s => s.CreateProjectStatus(It.Is<CreateProjectStatusDto>(d =>
                d.id_project == result.id_project && d.status_project == ProjectStatus.InProgress)), Times.Once);
        }

        // --- UpdateProject ---

        [Fact]
        public async Task UpdateProject_ShouldUpdateProvidedFields()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var project = BuildProject("old");
            context.Projects.Add(project);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateProject(project.id_project, new UpdateProjectDto
            {
                name_project = "renamed",
                description_project = "desc",
                url_project = "https://example.com"
            });

            Assert.Equal("renamed", result.name_project);
            Assert.Equal("desc", result.description_project);
            Assert.Equal("https://example.com", result.url_project);
            _projectStatusService.Verify(s => s.CreateProjectStatus(It.IsAny<CreateProjectStatusDto>()), Times.Never);
        }

        [Fact]
        public async Task UpdateProject_ShouldRecordStatusHistory_WhenStatusChanges()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var project = BuildProject();
            context.Projects.Add(project);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateProject(project.id_project, new UpdateProjectDto { status_project = ProjectStatus.Completed });

            Assert.Equal(ProjectStatus.Completed, result.status_project);
            _projectStatusService.Verify(s => s.CreateProjectStatus(It.Is<CreateProjectStatusDto>(d =>
                d.id_project == project.id_project && d.status_project == ProjectStatus.Completed)), Times.Once);
        }

        [Fact]
        public async Task UpdateProject_ShouldNotRecordStatusHistory_WhenStatusIsUnchanged()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var project = BuildProject(status: ProjectStatus.InProgress);
            context.Projects.Add(project);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.UpdateProject(project.id_project, new UpdateProjectDto { status_project = ProjectStatus.InProgress });

            _projectStatusService.Verify(s => s.CreateProjectStatus(It.IsAny<CreateProjectStatusDto>()), Times.Never);
        }

        [Fact]
        public async Task UpdateProject_ShouldThrowKeyNotFoundException_WhenProjectDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateProject(999, new UpdateProjectDto()));
        }

        // --- DeleteProject ---

        [Fact]
        public async Task DeleteProject_ShouldRemoveProject_AndDeleteDirectory()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var project = BuildProject();
            context.Projects.Add(project);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.DeleteProject(project.id_project);

            Assert.Equal(0, await context.Projects.CountAsync());
            _fileService.Verify(f => f.DeleteDirectory(Path.Combine("projectDocuments", project.id_project.ToString())), Times.Once);
        }

        [Fact]
        public async Task DeleteProject_ShouldThrowKeyNotFoundException_WhenProjectDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteProject(999));
            _fileService.Verify(f => f.DeleteDirectory(It.IsAny<string>()), Times.Never);
        }
    }
}
