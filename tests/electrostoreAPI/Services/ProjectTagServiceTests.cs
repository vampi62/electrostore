using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.ProjectTagService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class ProjectTagServiceTests : TestBase
    {
        private ProjectTagService CreateService(ApplicationDbContext context) =>
            new(_mapper, context);

        private static ProjectTags BuildProjectTag(string name = "tag", int weight = 0) => new()
        {
            name_project_tag = name,
            weight_project_tag = weight
        };

        private static Projects BuildProject(string name = "project") => new() { name_project = name };

        // --- GetProjectTags ---

        [Fact]
        public async Task GetProjectTags_ShouldReturnAllTags_WhenNoFilterApplied()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.ProjectTags.AddRange(BuildProjectTag("a"), BuildProjectTag("b"));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetProjectTags();

            Assert.Equal(2, result.pagination.total);
            Assert.Equal(2, result.data.Count());
        }

        [Fact]
        public async Task GetProjectTags_ShouldRestrictToIds_WhenIdResearchProvided()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var a = BuildProjectTag("a");
            var b = BuildProjectTag("b");
            var c = BuildProjectTag("c");
            context.ProjectTags.AddRange(a, b, c);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetProjectTags(idResearch: [a.id_project_tag, c.id_project_tag]);

            Assert.Equal(2, result.data.Count());
            Assert.DoesNotContain(result.data, t => t.id_project_tag == b.id_project_tag);
        }

        [Fact]
        public async Task GetProjectTags_ShouldReturnCountAndLinks_WhenExpanded()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tag = BuildProjectTag();
            var project = BuildProject();
            context.ProjectTags.Add(tag);
            context.Projects.Add(project);
            await context.SaveChangesAsync();
            context.ProjectsProjectTags.Add(new ProjectsProjectTags { id_project = project.id_project, id_project_tag = tag.id_project_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetProjectTags(expand: ["project_tags"]);

            var single = Assert.Single(result.data);
            Assert.Equal(1, single.project_tags_count);
            Assert.NotNull(single.project_tags);
            Assert.Single(single.project_tags!);
        }

        [Fact]
        public async Task GetProjectTags_ShouldNotLoadLinks_WhenNotExpanded()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tag = BuildProjectTag();
            var project = BuildProject();
            context.ProjectTags.Add(tag);
            context.Projects.Add(project);
            await context.SaveChangesAsync();
            context.ProjectsProjectTags.Add(new ProjectsProjectTags { id_project = project.id_project, id_project_tag = tag.id_project_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetProjectTags();

            var single = Assert.Single(result.data);
            Assert.Equal(1, single.project_tags_count);
            Assert.True(single.project_tags is null || !single.project_tags.Any());
        }

        // --- GetProjectTagById ---

        [Fact]
        public async Task GetProjectTagById_ShouldReturnTag_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tag = BuildProjectTag();
            context.ProjectTags.Add(tag);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetProjectTagById(tag.id_project_tag);

            Assert.Equal(tag.id_project_tag, result.id_project_tag);
            Assert.Equal(0, result.project_tags_count);
        }

        [Fact]
        public async Task GetProjectTagById_ShouldReturnLinks_WhenExpanded()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tag = BuildProjectTag();
            var project = BuildProject();
            context.ProjectTags.Add(tag);
            context.Projects.Add(project);
            await context.SaveChangesAsync();
            context.ProjectsProjectTags.Add(new ProjectsProjectTags { id_project = project.id_project, id_project_tag = tag.id_project_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetProjectTagById(tag.id_project_tag, ["project_tags"]);

            Assert.Equal(1, result.project_tags_count);
            Assert.Single(result.project_tags!);
        }

        [Fact]
        public async Task GetProjectTagById_ShouldThrowKeyNotFoundException_WhenTagDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetProjectTagById(999));
        }

        // --- CreateProjectTag ---

        [Fact]
        public async Task CreateProjectTag_ShouldPersistTag()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            var result = await service.CreateProjectTag(new CreateProjectTagDto { name_project_tag = "new-tag", weight_project_tag = 5 });

            Assert.Equal("new-tag", result.name_project_tag);
            Assert.Equal(5, result.weight_project_tag);
            Assert.Equal(1, await context.ProjectTags.CountAsync());
        }

        [Fact]
        public async Task CreateProjectTag_ShouldThrowInvalidOperationException_WhenNameAlreadyExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.ProjectTags.Add(BuildProjectTag("dup"));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateProjectTag(new CreateProjectTagDto { name_project_tag = "dup", weight_project_tag = 1 }));
        }

        // --- CreateBulkProjectTag ---

        [Fact]
        public async Task CreateBulkProjectTag_ShouldReturnValidAndErrorEntries()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.ProjectTags.Add(BuildProjectTag("existing"));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.CreateBulkProjectTag(
            [
                new CreateProjectTagDto { name_project_tag = "fresh", weight_project_tag = 1 },
                new CreateProjectTagDto { name_project_tag = "existing", weight_project_tag = 1 },
                new CreateProjectTagDto { name_project_tag = "fresh", weight_project_tag = 2 }
            ]);

            Assert.Single(result.valide);
            Assert.Equal(2, result.error.Count());
            Assert.Equal(2, await context.ProjectTags.CountAsync());
        }

        // --- UpdateProjectTag ---

        [Fact]
        public async Task UpdateProjectTag_ShouldUpdateProvidedFields()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tag = BuildProjectTag("a", 1);
            context.ProjectTags.Add(tag);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateProjectTag(tag.id_project_tag, new UpdateProjectTagDto { name_project_tag = "renamed", weight_project_tag = 9 });

            Assert.Equal("renamed", result.name_project_tag);
            Assert.Equal(9, result.weight_project_tag);
        }

        [Fact]
        public async Task UpdateProjectTag_ShouldKeepValues_WhenDtoIsEmpty()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tag = BuildProjectTag("a", 4);
            context.ProjectTags.Add(tag);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateProjectTag(tag.id_project_tag, new UpdateProjectTagDto());

            Assert.Equal("a", result.name_project_tag);
            Assert.Equal(4, result.weight_project_tag);
        }

        [Fact]
        public async Task UpdateProjectTag_ShouldAllowKeepingSameName()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tag = BuildProjectTag("same");
            context.ProjectTags.Add(tag);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateProjectTag(tag.id_project_tag, new UpdateProjectTagDto { name_project_tag = "same" });

            Assert.Equal("same", result.name_project_tag);
        }

        [Fact]
        public async Task UpdateProjectTag_ShouldThrowInvalidOperationException_WhenNewNameAlreadyUsedByAnotherTag()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tag1 = BuildProjectTag("a");
            var tag2 = BuildProjectTag("b");
            context.ProjectTags.AddRange(tag1, tag2);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.UpdateProjectTag(tag1.id_project_tag, new UpdateProjectTagDto { name_project_tag = "b" }));
        }

        [Fact]
        public async Task UpdateProjectTag_ShouldThrowKeyNotFoundException_WhenTagDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateProjectTag(999, new UpdateProjectTagDto()));
        }

        // --- DeleteProjectTag ---

        [Fact]
        public async Task DeleteProjectTag_ShouldRemoveTag_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tag = BuildProjectTag();
            context.ProjectTags.Add(tag);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.DeleteProjectTag(tag.id_project_tag);

            Assert.Equal(0, await context.ProjectTags.CountAsync());
        }

        [Fact]
        public async Task DeleteProjectTag_ShouldThrowKeyNotFoundException_WhenTagDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteProjectTag(999));
        }
    }
}
