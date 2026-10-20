using Microsoft.EntityFrameworkCore;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.EquipementTagService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class EquipementTagServiceTests : TestBase
    {
        private EquipementTagService CreateService(ApplicationDbContext context) =>
            new(_mapper, context);

        private static Equipements BuildEquipement(string reference = "eq") => new()
        {
            reference_name_equipement = reference,
            friendly_name_equipement = reference
        };

        private static Tags BuildTag(string name = "tag") => new() { name_tag = name };

        // --- GetEquipementsTagsByEquipementId ---

        [Fact]
        public async Task GetEquipementsTagsByEquipementId_ShouldReturnTagsLinkedToEquipement()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            var tag = BuildTag();
            context.Equipements.Add(equipement);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            context.EquipementsTags.Add(new EquipementsTags { id_equipement = equipement.id_equipement, id_tag = tag.id_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetEquipementsTagsByEquipementId(equipement.id_equipement);

            var single = Assert.Single(result.data);
            Assert.Equal(tag.id_tag, single.id_tag);
        }

        [Fact]
        public async Task GetEquipementsTagsByEquipementId_ShouldThrowKeyNotFoundException_WhenEquipementDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetEquipementsTagsByEquipementId(999));
        }

        // --- GetEquipementsTagsByTagId ---

        [Fact]
        public async Task GetEquipementsTagsByTagId_ShouldReturnEquipementsLinkedToTag()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            var tag = BuildTag();
            context.Equipements.Add(equipement);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            context.EquipementsTags.Add(new EquipementsTags { id_equipement = equipement.id_equipement, id_tag = tag.id_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetEquipementsTagsByTagId(tag.id_tag);

            var single = Assert.Single(result.data);
            Assert.Equal(equipement.id_equipement, single.id_equipement);
        }

        [Fact]
        public async Task GetEquipementsTagsByTagId_ShouldThrowKeyNotFoundException_WhenTagDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetEquipementsTagsByTagId(999));
        }

        // --- GetEquipementTagById ---

        [Fact]
        public async Task GetEquipementTagById_ShouldReturnEquipementTag_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            var tag = BuildTag();
            context.Equipements.Add(equipement);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            context.EquipementsTags.Add(new EquipementsTags { id_equipement = equipement.id_equipement, id_tag = tag.id_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetEquipementTagById(equipement.id_equipement, tag.id_tag);

            Assert.Equal(equipement.id_equipement, result.id_equipement);
            Assert.Equal(tag.id_tag, result.id_tag);
        }

        [Fact]
        public async Task GetEquipementTagById_ShouldThrowKeyNotFoundException_WhenLinkDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetEquipementTagById(1, 1));
        }

        // --- CreateEquipementTag ---

        [Fact]
        public async Task CreateEquipementTag_ShouldPersistLink_WhenEquipementAndTagExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            var tag = BuildTag();
            context.Equipements.Add(equipement);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateEquipementTagDto { id_equipement = equipement.id_equipement, id_tag = tag.id_tag };

            var result = await service.CreateEquipementTag(dto);

            Assert.Equal(equipement.id_equipement, result.id_equipement);
            Assert.Equal(1, await context.EquipementsTags.CountAsync());
        }

        [Fact]
        public async Task CreateEquipementTag_ShouldThrowKeyNotFoundException_WhenEquipementDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tag = BuildTag();
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateEquipementTagDto { id_equipement = 999, id_tag = tag.id_tag };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateEquipementTag(dto));
        }

        [Fact]
        public async Task CreateEquipementTag_ShouldThrowKeyNotFoundException_WhenTagDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            context.Equipements.Add(equipement);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateEquipementTagDto { id_equipement = equipement.id_equipement, id_tag = 999 };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateEquipementTag(dto));
        }

        [Fact]
        public async Task CreateEquipementTag_ShouldThrowInvalidOperationException_WhenLinkAlreadyExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            var tag = BuildTag();
            context.Equipements.Add(equipement);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            context.EquipementsTags.Add(new EquipementsTags { id_equipement = equipement.id_equipement, id_tag = tag.id_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateEquipementTagDto { id_equipement = equipement.id_equipement, id_tag = tag.id_tag };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateEquipementTag(dto));
        }

        // --- DeleteEquipementTag ---

        [Fact]
        public async Task DeleteEquipementTag_ShouldRemoveLink_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var equipement = BuildEquipement();
            var tag = BuildTag();
            context.Equipements.Add(equipement);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            context.EquipementsTags.Add(new EquipementsTags { id_equipement = equipement.id_equipement, id_tag = tag.id_tag });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.DeleteEquipementTag(equipement.id_equipement, tag.id_tag);

            Assert.Equal(0, await context.EquipementsTags.CountAsync());
        }

        [Fact]
        public async Task DeleteEquipementTag_ShouldThrowKeyNotFoundException_WhenLinkDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteEquipementTag(1, 1));
        }

        // --- expand / bulk operations ---

        private async Task<(int parentId, int tag1Id, int tag2Id)> SeedForBulk(ApplicationDbContext context)
        {
            var parent = BuildEquipement();
            context.Equipements.Add(parent);
            await context.SaveChangesAsync();
            var tag1 = BuildTag("bulk-1");
            var tag2 = BuildTag("bulk-2");
            context.Tags.AddRange(tag1, tag2);
            await context.SaveChangesAsync();
            return (parent.id_equipement, tag1.id_tag, tag2.id_tag);
        }

        [Fact]
        public async Task GetEquipementsTagsByEquipementId_ShouldExpandParentAndTag_WhenRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parentId, tagId, _) = await SeedForBulk(context);
            context.EquipementsTags.Add(new EquipementsTags { id_equipement = parentId, id_tag = tagId });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetEquipementsTagsByEquipementId(parentId, expand: ["equipement", "tag"]);

            var link = Assert.Single(result.data);
            Assert.NotNull(link.equipement);
            Assert.NotNull(link.tag);
        }

        [Fact]
        public async Task GetEquipementsTagsByTagId_ShouldExpandParentAndTag_WhenRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parentId, tagId, _) = await SeedForBulk(context);
            context.EquipementsTags.Add(new EquipementsTags { id_equipement = parentId, id_tag = tagId });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetEquipementsTagsByTagId(tagId, expand: ["equipement", "tag"]);

            var link = Assert.Single(result.data);
            Assert.NotNull(link.equipement);
            Assert.NotNull(link.tag);
        }

        [Fact]
        public async Task GetEquipementTagById_ShouldExpandParentAndTag_WhenRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parentId, tagId, _) = await SeedForBulk(context);
            context.EquipementsTags.Add(new EquipementsTags { id_equipement = parentId, id_tag = tagId });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetEquipementTagById(parentId, tagId, ["equipement", "tag"]);

            Assert.NotNull(result.equipement);
            Assert.NotNull(result.tag);
        }

        [Fact]
        public async Task CreateBulkEquipementTag_ShouldReturnValidAndErrorEntries()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parentId, tag1Id, _) = await SeedForBulk(context);
            var service = CreateService(context);

            var result = await service.CreateBulkEquipementTag(
            [
                new CreateEquipementTagDto { id_equipement = parentId, id_tag = tag1Id },
                new CreateEquipementTagDto { id_equipement = 9999, id_tag = tag1Id },
                new CreateEquipementTagDto { id_equipement = parentId, id_tag = tag1Id }
            ]);

            Assert.Single(result.valide);
            Assert.Equal(2, result.error.Count());
            Assert.Equal(1, await context.EquipementsTags.CountAsync());
        }

        [Fact]
        public async Task DeleteBulkEquipementTag_ShouldReturnValidAndErrorEntries()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var (parentId, tag1Id, tag2Id) = await SeedForBulk(context);
            context.EquipementsTags.Add(new EquipementsTags { id_equipement = parentId, id_tag = tag1Id });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.DeleteBulkEquipementTag(
            [
                new CreateEquipementTagDto { id_equipement = parentId, id_tag = tag1Id },
                new CreateEquipementTagDto { id_equipement = parentId, id_tag = tag2Id }
            ]);

            var deleted = Assert.Single(result.valide);
            Assert.Equal(tag1Id, deleted.id_tag);
            Assert.Single(result.error);
            Assert.Equal(0, await context.EquipementsTags.CountAsync());
        }
    }
}
