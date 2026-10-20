using Microsoft.EntityFrameworkCore;
using Xunit;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.ProjectItemService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class ProjectItemServiceTests : TestBase
    {
        private ProjectItemService CreateService(ApplicationDbContext context) =>
            new(_mapper, context);

        private static Projects BuildProject(string tag = "project") => new()
        {
            name_project = tag
        };

        private static Items BuildItem(string reference = "item") => new()
        {
            reference_name_item = reference,
            friendly_name_item = reference,
            threshold_min_item = 0
        };

        private static ProjectsItems BuildProjectItem(int parentId, int itemId, int quantity = 1) => new()
        {
            id_project = parentId,
            id_item = itemId,
            quantity_project_item = quantity
        };

        private static CreateProjectItemDto BuildCreateDto(int parentId, int itemId, int quantity = 2) => new()
        {
            id_project = parentId,
            id_item = itemId,
            quantity_project_item = quantity
        };

        private async Task<(Projects parent, Items item)> Seed(ApplicationDbContext context, bool link = false)
        {
            var parent = BuildProject();
            var item = BuildItem();
            context.Projects.Add(parent);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            if (link)
            {
                context.ProjectsItems.Add(BuildProjectItem(parent.id_project, item.id_item));
                await context.SaveChangesAsync();
            }
            return (parent, item);
        }

        // --- GetProjectItemsByProjectId ---

        [Fact]
        public async Task GetProjectItemsByProjectId_ShouldReturnOnlyItemsOfParent()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, item) = await Seed(context, link: true);
            var other = BuildProject("other");
            var otherItem = BuildItem("other");
            context.Projects.Add(other);
            context.Items.Add(otherItem);
            await context.SaveChangesAsync();
            context.ProjectsItems.Add(BuildProjectItem(other.id_project, otherItem.id_item));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetProjectItemsByProjectId(parent.id_project);

            var single = Assert.Single(result.data);
            Assert.Equal(item.id_item, single.id_item);
        }

        [Fact]
        public async Task GetProjectItemsByProjectId_ShouldExpandItemAndParent_WhenRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, _) = await Seed(context, link: true);
            var service = CreateService(context);

            var result = await service.GetProjectItemsByProjectId(parent.id_project, expand: ["item", "project"]);

            var single = Assert.Single(result.data);
            Assert.NotNull(single.item);
            Assert.NotNull(single.project);
        }

        [Fact]
        public async Task GetProjectItemsByProjectId_ShouldThrowKeyNotFoundException_WhenParentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetProjectItemsByProjectId(999));
        }

        // --- GetProjectItemsByItemId ---

        [Fact]
        public async Task GetProjectItemsByItemId_ShouldReturnParentsLinkedToItem()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, item) = await Seed(context, link: true);
            var service = CreateService(context);

            var result = await service.GetProjectItemsByItemId(item.id_item);

            var single = Assert.Single(result.data);
            Assert.Equal(parent.id_project, single.id_project);
        }

        [Fact]
        public async Task GetProjectItemsByItemId_ShouldExpandItemAndParent_WhenRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (_, item) = await Seed(context, link: true);
            var service = CreateService(context);

            var result = await service.GetProjectItemsByItemId(item.id_item, expand: ["item", "project"]);

            var single = Assert.Single(result.data);
            Assert.NotNull(single.item);
            Assert.NotNull(single.project);
        }

        [Fact]
        public async Task GetProjectItemsByItemId_ShouldThrowKeyNotFoundException_WhenItemDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetProjectItemsByItemId(999));
        }

        // --- GetProjectItemById ---

        [Fact]
        public async Task GetProjectItemById_ShouldReturnLink_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, item) = await Seed(context, link: true);
            var service = CreateService(context);

            var result = await service.GetProjectItemById(parent.id_project, item.id_item);

            Assert.Equal(1, result.quantity_project_item);
        }

        [Fact]
        public async Task GetProjectItemById_ShouldExpandItemAndParent_WhenRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, item) = await Seed(context, link: true);
            var service = CreateService(context);

            var result = await service.GetProjectItemById(parent.id_project, item.id_item, ["item", "project"]);

            Assert.NotNull(result.item);
            Assert.NotNull(result.project);
        }

        [Fact]
        public async Task GetProjectItemById_ShouldThrowKeyNotFoundException_WhenLinkDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetProjectItemById(1, 1));
        }

        // --- CreateProjectItem ---

        [Fact]
        public async Task CreateProjectItem_ShouldPersistLink()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, item) = await Seed(context);
            var service = CreateService(context);

            var result = await service.CreateProjectItem(BuildCreateDto(parent.id_project, item.id_item, 5));

            Assert.Equal(5, result.quantity_project_item);
            Assert.Equal(1, await context.ProjectsItems.CountAsync());
        }

        [Fact]
        public async Task CreateProjectItem_ShouldThrowKeyNotFoundException_WhenParentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem();
            context.Items.Add(item);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateProjectItem(BuildCreateDto(999, item.id_item)));
        }

        [Fact]
        public async Task CreateProjectItem_ShouldThrowKeyNotFoundException_WhenItemDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var parent = BuildProject();
            context.Projects.Add(parent);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateProjectItem(BuildCreateDto(parent.id_project, 999)));
        }

        [Fact]
        public async Task CreateProjectItem_ShouldThrowInvalidOperationException_WhenLinkAlreadyExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, item) = await Seed(context, link: true);
            var service = CreateService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateProjectItem(BuildCreateDto(parent.id_project, item.id_item)));
        }

        // --- CreateBulkProjectItem ---

        [Fact]
        public async Task CreateBulkProjectItem_ShouldReturnValidAndErrorEntries()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, item) = await Seed(context);
            var service = CreateService(context);

            var result = await service.CreateBulkProjectItem(
            [
                BuildCreateDto(parent.id_project, item.id_item),
                BuildCreateDto(parent.id_project, item.id_item),
                BuildCreateDto(999, item.id_item)
            ]);

            Assert.Single(result.valide);
            Assert.Equal(2, result.error.Count());
            Assert.Equal(1, await context.ProjectsItems.CountAsync());
        }

        // --- UpdateProjectItem ---

        [Fact]
        public async Task UpdateProjectItem_ShouldUpdateQuantity()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, item) = await Seed(context, link: true);
            var service = CreateService(context);

            var result = await service.UpdateProjectItem(parent.id_project, item.id_item, new UpdateProjectItemDto { quantity_project_item = 9 });

            Assert.Equal(9, result.quantity_project_item);
        }

        [Fact]
        public async Task UpdateProjectItem_ShouldKeepValues_WhenDtoIsEmpty()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, item) = await Seed(context, link: true);
            var service = CreateService(context);

            var result = await service.UpdateProjectItem(parent.id_project, item.id_item, new UpdateProjectItemDto());

            Assert.Equal(1, result.quantity_project_item);
        }

        [Fact]
        public async Task UpdateProjectItem_ShouldThrowKeyNotFoundException_WhenParentDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (_, item) = await Seed(context);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateProjectItem(999, item.id_item, new UpdateProjectItemDto()));
        }

        [Fact]
        public async Task UpdateProjectItem_ShouldThrowKeyNotFoundException_WhenItemDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, _) = await Seed(context);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateProjectItem(parent.id_project, 999, new UpdateProjectItemDto()));
        }

        [Fact]
        public async Task UpdateProjectItem_ShouldThrowKeyNotFoundException_WhenLinkDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, item) = await Seed(context);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateProjectItem(parent.id_project, item.id_item, new UpdateProjectItemDto()));
        }

        // --- DeleteProjectItem ---

        [Fact]
        public async Task DeleteProjectItem_ShouldRemoveLink()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parent, item) = await Seed(context, link: true);
            var service = CreateService(context);

            await service.DeleteProjectItem(parent.id_project, item.id_item);

            Assert.Equal(0, await context.ProjectsItems.CountAsync());
        }

        [Fact]
        public async Task DeleteProjectItem_ShouldThrowKeyNotFoundException_WhenLinkDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteProjectItem(1, 1));
        }
    }
}
