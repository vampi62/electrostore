using Microsoft.EntityFrameworkCore;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.TagService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class TagServiceTests : TestBase
    {
        private TagService CreateService(ApplicationDbContext context) =>
            new(_mapper, context);

        private static Tags BuildTag(string name = "tag", int weight = 0) => new()
        {
            name_tag = name,
            weight_tag = weight
        };

        // --- GetTags ---

        [Fact]
        public async Task GetTags_ShouldReturnAllTags_WhenNoFilterApplied()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Tags.AddRange(BuildTag("a"), BuildTag("b"));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetTags();

            Assert.Equal(2, result.pagination.total);
            Assert.Equal(2, result.data.Count());
        }

        // --- GetTagById ---

        [Fact]
        public async Task GetTagById_ShouldReturnTag_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tag = BuildTag();
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetTagById(tag.id_tag);

            Assert.Equal(tag.id_tag, result.id_tag);
            Assert.Equal(0, result.items_tags_count);
        }

        [Fact]
        public async Task GetTagById_ShouldThrowKeyNotFoundException_WhenTagDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetTagById(999));
        }

        // --- CreateTag ---

        [Fact]
        public async Task CreateTag_ShouldPersistTag()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateTagDto { name_tag = "new-tag", weight_tag = 5 };

            var result = await service.CreateTag(dto);

            Assert.Equal("new-tag", result.name_tag);
            Assert.Equal(1, await context.Tags.CountAsync());
        }

        [Fact]
        public async Task CreateTag_ShouldThrowInvalidOperationException_WhenNameAlreadyExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Tags.Add(BuildTag("dup"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateTagDto { name_tag = "dup", weight_tag = 5 };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateTag(dto));
        }

        // --- UpdateTag ---

        [Fact]
        public async Task UpdateTag_ShouldUpdateProvidedFields()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tag = BuildTag("a", 1);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new UpdateTagDto { name_tag = "renamed", weight_tag = 9 };

            var result = await service.UpdateTag(tag.id_tag, dto);

            Assert.Equal("renamed", result.name_tag);
            Assert.Equal(9, result.weight_tag);
        }

        [Fact]
        public async Task UpdateTag_ShouldThrowInvalidOperationException_WhenNewNameAlreadyUsedByAnotherTag()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tag1 = BuildTag("a");
            var tag2 = BuildTag("b");
            context.Tags.AddRange(tag1, tag2);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new UpdateTagDto { name_tag = "b" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateTag(tag1.id_tag, dto));
        }

        [Fact]
        public async Task UpdateTag_ShouldThrowKeyNotFoundException_WhenTagDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateTag(999, new UpdateTagDto()));
        }

        // --- DeleteTag ---

        [Fact]
        public async Task DeleteTag_ShouldRemoveTag_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tag = BuildTag();
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.DeleteTag(tag.id_tag);

            Assert.Equal(0, await context.Tags.CountAsync());
        }

        [Fact]
        public async Task DeleteTag_ShouldThrowKeyNotFoundException_WhenTagDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteTag(999));
        }

        // --- additional coverage: any filter, expand, bulk ---

        [Fact]
        public async Task GetTags_ShouldRestrictToIds_WhenAnyFilterProvided()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var a = BuildTag("a");
            var b = BuildTag("b");
            context.Tags.AddRange(a, b);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetTags(rsql: [new FilterDto { field = "id_tag", search_type = "any", value = b.id_tag.ToString() }]);

            var single = Assert.Single(result.data);
            Assert.Equal(b.id_tag, single.id_tag);
        }

        [Fact]
        public async Task GetTags_ShouldReturnCountsAndExpandedLinks()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tag = BuildTag();
            var item = new Items { reference_name_item = "i", friendly_name_item = "i", threshold_min_item = 0 };
            var store = new Stores { name_store = "s", mqtt_name_store = "m", xlength_store = 5, ylength_store = 5 };
            var equipement = new Equipements { reference_name_equipement = "e", friendly_name_equipement = "e" };
            context.Tags.Add(tag);
            context.Items.Add(item);
            context.Stores.Add(store);
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            var box = new Boxs { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 1, yend_box = 1 };
            context.Boxs.Add(box);
            await context.SaveChangesAsync();
            context.ItemsTags.Add(new ItemsTags { id_item = item.id_item, id_tag = tag.id_tag });
            context.StoresTags.Add(new StoresTags { id_store = store.id_store, id_tag = tag.id_tag });
            context.BoxsTags.Add(new BoxsTags { id_box = box.id_box, id_tag = tag.id_tag });
            context.EquipementsTags.Add(new EquipementsTags { id_equipement = equipement.id_equipement, id_tag = tag.id_tag });
            await context.SaveChangesAsync();
            using var queryContext = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(queryContext);

            var list = await service.GetTags(expand: ["stores_tags", "items_tags", "boxs_tags", "equipement_tags"]);
            var byId = await service.GetTagById(tag.id_tag, ["stores_tags", "items_tags", "boxs_tags", "equipement_tags"]);

            var single = Assert.Single(list.data);
            Assert.Equal(1, single.stores_tags_count);
            Assert.Equal(1, single.items_tags_count);
            Assert.Equal(1, single.boxs_tags_count);
            Assert.Equal(1, single.equipement_tags_count);
            Assert.Single(single.stores_tags!);
            Assert.Single(single.items_tags!);
            Assert.Single(single.boxs_tags!);
            Assert.Single(single.equipement_tags!);
            Assert.Single(byId.stores_tags!);
            Assert.Single(byId.items_tags!);
            Assert.Single(byId.boxs_tags!);
            Assert.Single(byId.equipement_tags!);
        }

        [Fact]
        public async Task CreateBulkTag_ShouldReturnValidAndErrorEntries()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Tags.Add(BuildTag("existing"));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.CreateBulkTag(
            [
                new CreateTagDto { name_tag = "fresh", weight_tag = 1 },
                new CreateTagDto { name_tag = "existing", weight_tag = 1 },
                new CreateTagDto { name_tag = "fresh", weight_tag = 2 }
            ]);

            Assert.Single(result.valide);
            Assert.Equal(2, result.error.Count);
            Assert.Equal(2, await context.Tags.CountAsync());
        }
    }
}
