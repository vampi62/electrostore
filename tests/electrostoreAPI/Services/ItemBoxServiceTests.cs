using Microsoft.EntityFrameworkCore;
using Xunit;
using electrostore;
using electrostore.Dto;
using electrostore.Models;
using electrostore.Services.ItemBoxService;
using electrostore.Tests.Utils;

namespace electrostore.Tests.Services
{
    public class ItemBoxServiceTests : TestBase
    {
        [Fact]
        public async Task GetByBox_ShouldReturnItems_AndCount()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            // Seed store, box, items
            context.Stores.Add(new Stores { id_store = 1, nom_store = "Store 1", xlength_store = 10, ylength_store = 10, mqtt_name_store = "s1" });
            context.Boxs.Add(new Boxs { id_box = 1, id_store = 1, xstart_box = 0, ystart_box = 0, xend_box = 1, yend_box = 1 });
            for (int i = 1; i <= 3; i++)
            {
                context.Items.Add(new Items
                {
                    id_item = i,
                    reference_name_item = $"REF-{i}",
                    friendly_name_item = $"Item {i}",
                    description_item = $"Desc {i}",
                    seuil_min_item = 1
                });
                context.ItemsBoxs.Add(new ItemsBoxs
                {
                    id_box = 1,
                    id_item = i,
                    qte_item_box = 10 + i,
                    seuil_max_item_item_box = 100 + i
                });
            }
            await context.SaveChangesAsync();

            var itemBoxService = new ItemBoxService(_mapper, context);

            var list = await itemBoxService.GetItemsBoxsByBoxId(1, 10, 0);
            var count = await itemBoxService.GetItemsBoxsCountByBoxId(1);

            Assert.NotNull(list);
            Assert.Equal(3, list.Count());
            Assert.Equal(3, count);
        }

        [Fact]
        public async Task GetByItem_ShouldReturnBoxs_AndCount()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            // Seed store, two boxs and one item
            context.Stores.Add(new Stores { id_store = 2, nom_store = "Store 2", xlength_store = 10, ylength_store = 10, mqtt_name_store = "s2" });
            context.Boxs.AddRange(
                new Boxs { id_box = 2, id_store = 2, xstart_box = 0, ystart_box = 0, xend_box = 1, yend_box = 1 },
                new Boxs { id_box = 3, id_store = 2, xstart_box = 2, ystart_box = 2, xend_box = 3, yend_box = 3 }
            );
            context.Items.Add(new Items { id_item = 10, reference_name_item = "REF-10", friendly_name_item = "Item 10", description_item = "Desc 10", seuil_min_item = 1 });
            context.ItemsBoxs.AddRange(
                new ItemsBoxs { id_box = 2, id_item = 10, qte_item_box = 5, seuil_max_item_item_box = 50 },
                new ItemsBoxs { id_box = 3, id_item = 10, qte_item_box = 6, seuil_max_item_item_box = 60 }
            );
            await context.SaveChangesAsync();

            var itemBoxService = new ItemBoxService(_mapper, context);

            var list = await itemBoxService.GetItemsBoxsByItemId(10, 10, 0);
            var count = await itemBoxService.GetItemsBoxsCountByItemId(10);

            Assert.NotNull(list);
            Assert.Equal(2, list.Count());
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task GetById_ShouldReturnSingleItemBox()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Stores.Add(new Stores { id_store = 4, nom_store = "Store 4", xlength_store = 10, ylength_store = 10, mqtt_name_store = "s4" });
            context.Boxs.Add(new Boxs { id_box = 4, id_store = 4, xstart_box = 0, ystart_box = 0, xend_box = 1, yend_box = 1 });
            context.Items.Add(new Items { id_item = 20, reference_name_item = "REF-20", friendly_name_item = "Item 20", description_item = "Desc 20", seuil_min_item = 1 });
            context.ItemsBoxs.Add(new ItemsBoxs { id_box = 4, id_item = 20, qte_item_box = 7, seuil_max_item_item_box = 70 });
            await context.SaveChangesAsync();

            var itemBoxService = new ItemBoxService(_mapper, context);
            var ib = await itemBoxService.GetItemBoxById(20, 4);

            Assert.NotNull(ib);
            Assert.Equal(4, ib.id_box);
            Assert.Equal(20, ib.id_item);
            Assert.Equal(7, ib.qte_item_box);
            Assert.Equal(70, ib.seuil_max_item_item_box);
        }

        [Fact]
        public async Task Create_ShouldCreateItemBox()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Stores.Add(new Stores { id_store = 5, nom_store = "Store 5", xlength_store = 10, ylength_store = 10, mqtt_name_store = "s5" });
            context.Boxs.Add(new Boxs { id_box = 5, id_store = 5, xstart_box = 0, ystart_box = 0, xend_box = 1, yend_box = 1 });
            context.Items.Add(new Items { id_item = 30, reference_name_item = "REF-30", friendly_name_item = "Item 30", description_item = "Desc 30", seuil_min_item = 1 });
            await context.SaveChangesAsync();

            var itemBoxService = new ItemBoxService(_mapper, context);

            var dto = new CreateItemBoxDto
            {
                id_box = 5,
                id_item = 30,
                qte_item_box = 9,
                seuil_max_item_item_box = 90
            };

            var created = await itemBoxService.CreateItemBox(dto);

            Assert.NotNull(created);
            Assert.Equal(5, created.id_box);
            Assert.Equal(30, created.id_item);
            Assert.Equal(9, created.qte_item_box);
            Assert.Equal(90, created.seuil_max_item_item_box);

            var inDb = await context.ItemsBoxs.FindAsync(30, 5);
            Assert.NotNull(inDb);
        }

        [Fact]
        public async Task Update_ShouldModifyQteAndSeuilMax()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Stores.Add(new Stores { id_store = 6, nom_store = "Store 6", xlength_store = 10, ylength_store = 10, mqtt_name_store = "s6" });
            context.Boxs.Add(new Boxs { id_box = 6, id_store = 6, xstart_box = 0, ystart_box = 0, xend_box = 1, yend_box = 1 });
            context.Items.Add(new Items { id_item = 40, reference_name_item = "REF-40", friendly_name_item = "Item 40", description_item = "Desc 40", seuil_min_item = 1 });
            context.ItemsBoxs.Add(new ItemsBoxs { id_box = 6, id_item = 40, qte_item_box = 1, seuil_max_item_item_box = 10 });
            await context.SaveChangesAsync();

            var itemBoxService = new ItemBoxService(_mapper, context);

            var updated = await itemBoxService.UpdateItemBox(40, 6, new UpdateItemBoxDto
            {
                qte_item_box = 4,
                seuil_max_item_item_box = 44
            });

            Assert.NotNull(updated);
            Assert.Equal(4, updated.qte_item_box);
            Assert.Equal(44, updated.seuil_max_item_item_box);

            var inDb = await context.ItemsBoxs.FindAsync(40, 6);
            Assert.NotNull(inDb);
            Assert.Equal(4, inDb!.qte_item_box);
            Assert.Equal(44, inDb!.seuil_max_item_item_box);
        }

        [Fact]
        public async Task Delete_ShouldRemoveItemBox()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Stores.Add(new Stores { id_store = 7, nom_store = "Store 7", xlength_store = 10, ylength_store = 10, mqtt_name_store = "s7" });
            context.Boxs.Add(new Boxs { id_box = 7, id_store = 7, xstart_box = 0, ystart_box = 0, xend_box = 1, yend_box = 1 });
            context.Items.Add(new Items { id_item = 50, reference_name_item = "REF-50", friendly_name_item = "Item 50", description_item = "Desc 50", seuil_min_item = 1 });
            context.ItemsBoxs.Add(new ItemsBoxs { id_box = 7, id_item = 50, qte_item_box = 2, seuil_max_item_item_box = 20 });
            await context.SaveChangesAsync();

            var itemBoxService = new ItemBoxService(_mapper, context);

            await itemBoxService.DeleteItemBox(50, 7);

            var inDb = await context.ItemsBoxs.FindAsync(50, 7);
            Assert.Null(inDb);
        }

        [Fact]
        public async Task CheckIfStoreExists_ShouldSucceed_WhenBoxInStore()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);

            context.Stores.Add(new Stores { id_store = 8, nom_store = "Store 8", xlength_store = 10, ylength_store = 10, mqtt_name_store = "s8" });
            context.Boxs.Add(new Boxs { id_box = 8, id_store = 8, xstart_box = 0, ystart_box = 0, xend_box = 1, yend_box = 1 });
            await context.SaveChangesAsync();

            var itemBoxService = new ItemBoxService(_mapper, context);

            await itemBoxService.CheckIfStoreExists(8, 8);
        }
    }
}