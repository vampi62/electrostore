using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Services.ItemHistoryService;
using ElectrostoreAPI.Services.ItemService;
using ElectrostoreAPI.Tests.Utils;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ElectrostoreAPI.Tests.Services
{
    public class ItemServiceTests : TestBase
    {
        private readonly Mock<IFileService> _fileService = new();
        private readonly Mock<IItemHistoryService> _itemHistoryService = new();

        private ItemService CreateService(ApplicationDbContext context) =>
            new(_mapper, context, _fileService.Object, _itemHistoryService.Object);

        private static Items BuildItem(string reference, int threshold) => new()
        {
            reference_name_item = reference,
            friendly_name_item = reference,
            threshold_min_item = threshold
        };

        [Fact]
        public async Task GetLowStockItemsAsync_ShouldReturnOnlyItems_WithQuantityBelowThreshold()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var lowItem = BuildItem("LOW", 10);
            var okItem = BuildItem("OK", 10);
            context.Items.AddRange(lowItem, okItem);
            await context.SaveChangesAsync();
            context.ItemsBoxs.AddRange(
                new ItemsBoxs { id_item = lowItem.id_item, id_box = 1, quantity_item_box = 2 },
                new ItemsBoxs { id_item = okItem.id_item, id_box = 1, quantity_item_box = 20 });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            // Act
            var result = (await service.GetLowStockItemsAsync()).ToList();

            // Assert
            var single = Assert.Single(result);
            Assert.Equal("LOW", single.reference_name_item);
            Assert.Equal(2, single.quantity_item);
        }

        [Fact]
        public async Task GetLowStockItemsAsync_ShouldIgnoreItems_WithNoThresholdConfigured()
        {
            // Arrange - threshold_min_item == 0 means "no alert configured" for this item.
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem("NOALERT", 0);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            context.ItemsBoxs.Add(new ItemsBoxs { id_item = item.id_item, id_box = 1, quantity_item_box = 0 });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            // Act
            var result = await service.GetLowStockItemsAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetLowStockItemsAsync_ShouldReturnAllLowItems_WhenSinceDateIsNotProvided()
        {
            // Arrange - default "full summary" behaviour, regardless of history.
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem("LOW", 10);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            context.ItemsBoxs.Add(new ItemsBoxs { id_item = item.id_item, id_box = 1, quantity_item_box = 1 });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            // Act
            var result = await service.GetLowStockItemsAsync();

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public async Task GetLowStockItemsAsync_ShouldOnlyReturnItems_WithRecentQuantityChange_WhenSinceDateIsProvided()
        {
            // Arrange - two items below threshold, only one has a recent quantity-changing history entry.
            using var context = new ApplicationDbContext(_dbContextOptions);
            var recentlyChanged = BuildItem("RECENT", 10);
            var staleItem = BuildItem("STALE", 10);
            context.Items.AddRange(recentlyChanged, staleItem);
            await context.SaveChangesAsync();
            context.ItemsBoxs.AddRange(
                new ItemsBoxs { id_item = recentlyChanged.id_item, id_box = 1, quantity_item_box = 1 },
                new ItemsBoxs { id_item = staleItem.id_item, id_box = 1, quantity_item_box = 1 });
            var recentHistory = new ItemsHistory { id_item = recentlyChanged.id_item, type_item_history = ItemHistoryType.StockRemoved };
            var staleHistory = new ItemsHistory { id_item = staleItem.id_item, type_item_history = ItemHistoryType.StockRemoved };
            context.ItemsHistory.AddRange(recentHistory, staleHistory);
            await context.SaveChangesAsync();
            // ApplicationDbContext.AddTimestamps stamps created_at to "now" on insert, overriding
            // any value set beforehand - update it in a second save (State becomes Modified, which
            // only re-stamps updated_at) to simulate history entries from different dates.
            recentHistory.created_at = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
            staleHistory.created_at = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            // Act
            var result = (await service.GetLowStockItemsAsync(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc))).ToList();

            // Assert
            var single = Assert.Single(result);
            Assert.Equal("RECENT", single.reference_name_item);
        }

        [Fact]
        public async Task GetLowStockItemsAsync_ShouldIgnoreHistoryEntries_NotRelatedToQuantityChanges()
        {
            // Arrange - an item below threshold whose only recent history entry is a metadata
            // update (not a quantity change) must be excluded when sinceDate is provided.
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem("METADATA-ONLY", 10);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            context.ItemsBoxs.Add(new ItemsBoxs { id_item = item.id_item, id_box = 1, quantity_item_box = 1 });
            var history = new ItemsHistory { id_item = item.id_item, type_item_history = ItemHistoryType.ItemUpdated };
            context.ItemsHistory.Add(history);
            await context.SaveChangesAsync();
            history.created_at = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            // Act
            var result = await service.GetLowStockItemsAsync(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));

            // Assert
            Assert.Empty(result);
        }

        // --- GetItems ---

        [Fact]
        public async Task GetItems_ShouldReturnAllItems_WhenNoFilterApplied()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.AddRange(BuildItem("a", 10), BuildItem("b", 10));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            // Act
            var result = await service.GetItems();

            // Assert
            Assert.Equal(2, result.pagination.total);
            Assert.Equal(2, result.data.Count());
        }

        // --- GetItemById ---

        [Fact]
        public async Task GetItemById_ShouldReturnItem_WhenItExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem("a", 10);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            // Act
            var result = await service.GetItemById(item.id_item);

            // Assert
            Assert.Equal(item.id_item, result.id_item);
            Assert.Equal("a", result.reference_name_item);
        }

        [Fact]
        public async Task GetItemById_ShouldThrowKeyNotFoundException_WhenItemDoesNotExist()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetItemById(999));
        }

        // --- CreateItem ---

        [Fact]
        public async Task CreateItem_ShouldPersistItem_AndCreateDirectories()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateItemDto { reference_name_item = "new-item", friendly_name_item = "New item", threshold_min_item = 5 };

            // Act
            var result = await service.CreateItem(dto);

            // Assert
            Assert.Equal("new-item", result.reference_name_item);
            Assert.Equal(1, await context.Items.CountAsync());
            _fileService.Verify(f => f.CreateDirectory(It.IsAny<string>()), Times.Exactly(3));
            _itemHistoryService.Verify(h => h.LogHistory(result.id_item, null, ItemHistoryType.ItemCreated, null, null, null), Times.Once);
        }

        [Fact]
        public async Task CreateItem_ShouldThrowInvalidOperationException_WhenReferenceNameAlreadyExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem("dup", 10));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateItemDto { reference_name_item = "dup", friendly_name_item = "Dup item", threshold_min_item = 5 };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateItem(dto));
        }

        // --- UpdateItem ---

        [Fact]
        public async Task UpdateItem_ShouldUpdateProvidedFields()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem("a", 10);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new UpdateItemDto { friendly_name_item = "renamed", threshold_min_item = 20 };

            // Act
            var result = await service.UpdateItem(item.id_item, dto);

            // Assert
            Assert.Equal("renamed", result.friendly_name_item);
            Assert.Equal(20, result.threshold_min_item);
            Assert.Equal("a", result.reference_name_item);
            _itemHistoryService.Verify(h => h.LogHistory(item.id_item, null, ItemHistoryType.ItemUpdated, null, null, null), Times.Once);
        }

        [Fact]
        public async Task UpdateItem_ShouldThrowInvalidOperationException_WhenNewReferenceNameAlreadyUsedByAnotherItem()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item1 = BuildItem("a", 10);
            var item2 = BuildItem("b", 10);
            context.Items.AddRange(item1, item2);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new UpdateItemDto { reference_name_item = "b" };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateItem(item1.id_item, dto));
        }

        [Fact]
        public async Task UpdateItem_ShouldThrowKeyNotFoundException_WhenItemDoesNotExist()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateItem(999, new UpdateItemDto()));
        }

        // --- DeleteItem ---

        [Fact]
        public async Task DeleteItem_ShouldRemoveItem_AndDeleteDirectories()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem("a", 10);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            // Act
            await service.DeleteItem(item.id_item);

            // Assert
            Assert.Equal(0, await context.Items.CountAsync());
            _fileService.Verify(f => f.DeleteDirectory(It.IsAny<string>()), Times.Exactly(3));
            _itemHistoryService.Verify(h => h.LogHistory(item.id_item, null, ItemHistoryType.ItemDeleted, null, null, null), Times.Once);
        }

        [Fact]
        public async Task DeleteItem_ShouldThrowKeyNotFoundException_WhenItemDoesNotExist()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteItem(999));
        }

        // --- additional coverage: lists, expand, images ---

        private static Mock<Microsoft.AspNetCore.Http.IFormFile> BuildImageFile()
        {
            var file = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
            file.Setup(f => f.FileName).Returns("pic.png");
            file.Setup(f => f.ContentType).Returns("image/png");
            file.Setup(f => f.OpenReadStream()).Returns(() => new System.IO.MemoryStream([1, 2, 3]));
            return file;
        }

        private void SetupImageSaving()
        {
            _fileService.SetupImageWithThumbnailSaving("itemImages/1/pic.png", "itemImagesThumbnails/1/pic.png");
        }

        private async Task<Items> SeedItemWithRelations(ApplicationDbContext context)
        {
            var item = BuildItem("full", 10);
            var tag = new Tags { name_tag = "tag" };
            var store = new Stores { name_store = "s", mqtt_name_store = "s-mqtt", xlength_store = 10, ylength_store = 10 };
            var command = new Commands { url_command = "https://x", date_command = DateTime.UtcNow };
            var project = new Projects { name_project = "p" };
            context.Items.Add(item);
            context.Tags.Add(tag);
            context.Stores.Add(store);
            context.Commands.Add(command);
            context.Projects.Add(project);
            await context.SaveChangesAsync();
            var box = new Boxs { id_store = store.id_store, xstart_box = 0, ystart_box = 0, xend_box = 1, yend_box = 1 };
            context.Boxs.Add(box);
            await context.SaveChangesAsync();
            context.ItemsTags.Add(new ItemsTags { id_item = item.id_item, id_tag = tag.id_tag });
            context.ItemsBoxs.Add(new ItemsBoxs { id_item = item.id_item, id_box = box.id_box, quantity_item_box = 4, threshold_max_item_item_box = 10 });
            context.CommandsItems.Add(new CommandsItems { id_item = item.id_item, id_command = command.id_command, quantity_command_item = 1, price_command_item = 1f });
            context.ProjectsItems.Add(new ProjectsItems { id_item = item.id_item, id_project = project.id_project, quantity_project_item = 1 });
            context.ItemsDocuments.Add(new ItemsDocuments
            {
                id_item = item.id_item,
                url_item_document = "i/doc.pdf",
                name_item_document = "doc",
                type_item_document = "application/pdf",
                size_item_document = 1
            });
            context.ItemsHistory.Add(new ItemsHistory { id_item = item.id_item, type_item_history = ItemHistoryType.ItemCreated });
            await context.SaveChangesAsync();
            return item;
        }

        [Fact]
        public async Task GetItems_ShouldRestrictToIds_WhenAnyFilterProvided()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var a = BuildItem("a", 1);
            var b = BuildItem("b", 1);
            context.Items.AddRange(a, b);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetItems(rsql: [new FilterDto { field = "id_item", search_type = "any", value = b.id_item.ToString() }]);

            var single = Assert.Single(result.data);
            Assert.Equal(b.id_item, single.id_item);
        }

        [Fact]
        public async Task GetItems_ShouldReturnCountsQuantityAndExpandedRelations()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            await SeedItemWithRelations(context);
            using var queryContext = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(queryContext);

            var result = await service.GetItems(expand: ["item_tags", "item_boxs", "command_items", "project_items", "item_documents", "item_history"]);

            var single = Assert.Single(result.data);
            Assert.Equal(4, single.quantity_item);
            Assert.Equal(1, single.item_tags_count);
            Assert.Equal(1, single.item_boxs_count);
            Assert.Equal(1, single.command_items_count);
            Assert.Equal(1, single.project_items_count);
            Assert.Equal(1, single.item_documents_count);
            Assert.Single(single.item_tags!);
            Assert.Single(single.item_boxs!);
            Assert.Single(single.command_items!);
            Assert.Single(single.project_items!);
            Assert.Single(single.item_documents!);
            Assert.Single(single.item_history!);
        }

        [Fact]
        public async Task GetItemById_ShouldReturnCountsQuantityAndExpandedRelations()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = await SeedItemWithRelations(context);
            using var queryContext = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(queryContext);

            var result = await service.GetItemById(item.id_item, ["item_tags", "item_boxs", "command_items", "project_items", "item_documents", "item_history"]);

            Assert.Equal(4, result.quantity_item);
            Assert.Single(result.item_tags!);
            Assert.Single(result.item_boxs!);
            Assert.Single(result.command_items!);
            Assert.Single(result.project_items!);
            Assert.Single(result.item_documents!);
            Assert.Single(result.item_history!);
        }

        [Fact]
        public async Task CreateItem_ShouldSaveImageAndThumbnail_WhenImageProvided()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetupImageSaving();
            var service = CreateService(context);

            var result = await service.CreateItem(new CreateItemDto
            {
                reference_name_item = "img",
                friendly_name_item = "img",
                threshold_min_item = 1,
                img_file = BuildImageFile().Object
            });

            Assert.Equal("itemImages/1/pic.png", result.url_picture_item);
            Assert.Equal("itemImagesThumbnails/1/pic.png", result.url_thumbnail_item);
        }

        [Fact]
        public async Task UpdateItem_ShouldNotLogHistory_WhenNothingChanges()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem("a", 10);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.UpdateItem(item.id_item, new UpdateItemDto
            {
                reference_name_item = "a",
                friendly_name_item = "a",
                threshold_min_item = 10,
                description_item = string.Empty
            });

            _itemHistoryService.Verify(h => h.LogHistory(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<ItemHistoryType>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task UpdateItem_ShouldUpdateReferenceAndDescription()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem("a", 10);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateItem(item.id_item, new UpdateItemDto { reference_name_item = "b", description_item = "desc" });

            Assert.Equal("b", result.reference_name_item);
            Assert.Equal("desc", result.description_item);
        }

        [Fact]
        public async Task UpdateItem_ShouldReplaceImage_AndDeleteOldFiles()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem("a", 10);
            item.url_picture_item = "itemImages/1/old.png";
            item.url_thumbnail_item = "itemImagesThumbnails/1/old.png";
            context.Items.Add(item);
            await context.SaveChangesAsync();
            SetupImageSaving();
            var service = CreateService(context);

            var result = await service.UpdateItem(item.id_item, new UpdateItemDto { img_file = BuildImageFile().Object });

            Assert.Equal("itemImages/1/pic.png", result.url_picture_item);
            _fileService.Verify(f => f.DeleteFile("itemImages/1/old.png"), Times.Once);
            _fileService.Verify(f => f.DeleteFile("itemImagesThumbnails/1/old.png"), Times.Once);
            _itemHistoryService.Verify(h => h.LogHistory(item.id_item, null, ItemHistoryType.ItemUpdated, null, null, null), Times.Once);
        }

        [Fact]
        public async Task UpdateItem_ShouldAddImage_WhenItemHadNone()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem("a", 10);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            SetupImageSaving();
            var service = CreateService(context);

            var result = await service.UpdateItem(item.id_item, new UpdateItemDto { img_file = BuildImageFile().Object });

            Assert.NotNull(result.url_picture_item);
            _fileService.Verify(f => f.DeleteFile(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateItem_ShouldRemoveImage_WhenUnsetRequested()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var item = BuildItem("a", 10);
            item.url_picture_item = "itemImages/1/old.png";
            item.url_thumbnail_item = "itemImagesThumbnails/1/old.png";
            context.Items.Add(item);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.UpdateItem(item.id_item, new UpdateItemDto { unset_img_item = true });

            Assert.Null(result.url_picture_item);
            Assert.Null(result.url_thumbnail_item);
            _fileService.Verify(f => f.DeleteFile("itemImages/1/old.png"), Times.Once);
            _fileService.Verify(f => f.DeleteFile("itemImagesThumbnails/1/old.png"), Times.Once);
        }
    }
}
