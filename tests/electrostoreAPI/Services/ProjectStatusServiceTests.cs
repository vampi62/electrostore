using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.ProjectStatusService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class ProjectStatusServiceTests : TestBase
    {
        private ProjectStatusService CreateService(ApplicationDbContext context) =>
            new(_mapper, context);

        private static Projects BuildProject(string name = "project") => new() { name_project = name };

        // --- GetProjectStatusByProjectId ---

        [Fact]
        public async Task GetProjectStatusByProjectId_ShouldReturnOnlyStatusOfProject()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var project = BuildProject("a");
            var other = BuildProject("b");
            context.Projects.AddRange(project, other);
            await context.SaveChangesAsync();
            context.ProjectsStatus.AddRange(
                new ProjectsStatus { id_project = project.id_project, status_project = ProjectStatus.NotStarted },
                new ProjectsStatus { id_project = project.id_project, status_project = ProjectStatus.InProgress },
                new ProjectsStatus { id_project = other.id_project, status_project = ProjectStatus.Completed });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetProjectStatusByProjectId(project.id_project);

            Assert.Equal(2, result.data.Count());
            Assert.All(result.data, s => Assert.Equal(project.id_project, s.id_project));
        }

        [Fact]
        public async Task GetProjectStatusByProjectId_ShouldThrowKeyNotFoundException_WhenProjectDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetProjectStatusByProjectId(999));
        }

        // --- GetProjectStatusById ---

        [Fact]
        public async Task GetProjectStatusById_ShouldReturnStatus_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var project = BuildProject();
            context.Projects.Add(project);
            await context.SaveChangesAsync();
            var status = new ProjectsStatus { id_project = project.id_project, status_project = ProjectStatus.InProgress };
            context.ProjectsStatus.Add(status);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetProjectStatusById(status.id_project_status);

            Assert.Equal(status.id_project_status, result.id_project_status);
            Assert.Equal(ProjectStatus.InProgress, result.status_project);
        }

        [Fact]
        public async Task GetProjectStatusById_ShouldReturnStatus_WhenProjectFilterMatches()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var project = BuildProject();
            context.Projects.Add(project);
            await context.SaveChangesAsync();
            var status = new ProjectsStatus { id_project = project.id_project, status_project = ProjectStatus.NotStarted };
            context.ProjectsStatus.Add(status);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetProjectStatusById(status.id_project_status, project.id_project);

            Assert.Equal(project.id_project, result.id_project);
        }

        [Fact]
        public async Task GetProjectStatusById_ShouldThrowKeyNotFoundException_WhenProjectFilterDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var project = BuildProject();
            context.Projects.Add(project);
            await context.SaveChangesAsync();
            var status = new ProjectsStatus { id_project = project.id_project, status_project = ProjectStatus.NotStarted };
            context.ProjectsStatus.Add(status);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetProjectStatusById(status.id_project_status, project.id_project + 1));
        }

        [Fact]
        public async Task GetProjectStatusById_ShouldThrowKeyNotFoundException_WhenStatusDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetProjectStatusById(999));
        }

        // --- CreateProjectStatus ---

        [Fact]
        public async Task CreateProjectStatus_ShouldPersistStatus()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var project = BuildProject();
            context.Projects.Add(project);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.CreateProjectStatus(new CreateProjectStatusDto { id_project = project.id_project, status_project = ProjectStatus.Completed });

            Assert.Equal(ProjectStatus.Completed, result.status_project);
            Assert.Equal(1, await context.ProjectsStatus.CountAsync());
        }

        [Fact]
        public async Task CreateProjectStatus_ShouldThrowKeyNotFoundException_WhenProjectDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.CreateProjectStatus(new CreateProjectStatusDto { id_project = 999, status_project = ProjectStatus.NotStarted }));
        }
    }
}
