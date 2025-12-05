using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using electrostore.Controllers;
using electrostore.Dto;
using electrostore.Services.ItemService;

namespace electrostore.Tests.Controllers
{
    public class ItemControllerTests
    {
        private readonly Mock<IItemService> _mockItemService;
        private readonly ItemController _controller;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<HttpResponse> _mockHttpResponse;
        private readonly HeaderDictionary _headerDictionary;

        public ItemControllerTests()
        {
            _mockItemService = new Mock<IItemService>();
            
            // Set up mock HttpContext and Response
            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpResponse = new Mock<HttpResponse>();
            _headerDictionary = new HeaderDictionary();
            
            _mockHttpResponse.Setup(r => r.Headers).Returns(_headerDictionary);
            _mockHttpContext.Setup(c => c.Response).Returns(_mockHttpResponse.Object);
            
            _controller = new ItemController(_mockItemService.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _mockHttpContext.Object
            };
        }

        [Fact]
        public async Task GetItems_ReturnsOkResult_WithListOfItems()
        {
            // Arrange
            var items = new List<ReadExtendedItemDto>
            {
                new ReadExtendedItemDto { id_item = 1, reference_name_item = "Item1", friendly_name_item = "Friendly Item 1", seuil_min_item = 10, description_item = "Description 1", id_img = 1, created_at = DateTime.Now, updated_at = DateTime.Now,
                    item_tags_count = 0, item_boxs_count = 0, command_items_count = 0, projet_items_count = 0, item_documents_count = 0 },
                new ReadExtendedItemDto { id_item = 2, reference_name_item = "Item2", friendly_name_item = "Friendly Item 2", seuil_min_item = 20, description_item = "Description 2", id_img = 2, created_at = DateTime.Now, updated_at = DateTime.Now,
                    item_tags_count = 0, item_boxs_count = 0, command_items_count = 0, projet_items_count = 0, item_documents_count = 0 }
            };
            _mockItemService.Setup(service => service.GetItems(100, 0, null, null))
                .ReturnsAsync(items);
            _mockItemService.Setup(service => service.GetItemsCount())
                .ReturnsAsync(items.Count);

            // Act
            var result = await _controller.GetItems();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedItems = Assert.IsAssignableFrom<IEnumerable<ReadExtendedItemDto>>(okResult.Value);
            Assert.Equal(2, returnedItems.Count());
        }

        [Fact]
        public async Task GetItemById_ReturnsOkResult_WithItem()
        {
            // Arrange
            var itemId = 1;
            var item = new ReadExtendedItemDto { id_item = itemId, reference_name_item = "Item1", friendly_name_item = "Friendly Item 1", seuil_min_item = 10, description_item = "Description 1", id_img = 1, created_at = DateTime.Now, updated_at = DateTime.Now,
                    item_tags_count = 0, item_boxs_count = 0, command_items_count = 0, projet_items_count = 0, item_documents_count = 0 };
            _mockItemService.Setup(service => service.GetItemById(itemId, It.IsAny<List<string>>()))
                .ReturnsAsync(item);

            // Act
            var result = await _controller.GetItemById(itemId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedItem = Assert.IsType<ReadExtendedItemDto>(okResult.Value);
            Assert.Equal(itemId, returnedItem.id_item);
        }

        [Fact]
        public async Task CreateItem_ReturnsCreatedAtActionResult_WithCreatedItem()
        {
            // Arrange
            var createItemDto = new CreateItemDto { reference_name_item = "NewItem", friendly_name_item = "New Friendly Item", seuil_min_item = 15, description_item = "New Description", id_img = 3 };
            var createdItem = new ReadItemDto { id_item = 3, reference_name_item = "NewItem", friendly_name_item = "New Friendly Item", seuil_min_item = 15, description_item = "New Description", id_img = 3, created_at = DateTime.Now, updated_at = DateTime.Now };
            _mockItemService.Setup(service => service.CreateItem(createItemDto))
                .ReturnsAsync(createdItem);

            // Act
            var result = await _controller.CreateItem(createItemDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedItem = Assert.IsType<ReadItemDto>(createdAtActionResult.Value);
            Assert.Equal(createdItem.id_item, returnedItem.id_item);
        }

        [Fact]
        public async Task UpdateItem_ReturnsOkResult_WithUpdatedItem()
        {
            // Arrange
            var itemId = 1;
            var updateItemDto = new UpdateItemDto { reference_name_item = "UpdatedItem", friendly_name_item = "Updated Friendly Item", seuil_min_item = 20, description_item = "Updated Description", id_img = 4 };
            var updatedItem = new ReadItemDto { id_item = itemId, reference_name_item = "UpdatedItem", friendly_name_item = "Updated Friendly Item", seuil_min_item = 20, description_item = "Updated Description", id_img = 4, created_at = DateTime.Now.AddDays(-1), updated_at = DateTime.Now };
            _mockItemService.Setup(service => service.UpdateItem(itemId, updateItemDto))
                .ReturnsAsync(updatedItem);

            // Act
            var result = await _controller.UpdateItem(itemId, updateItemDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedItem = Assert.IsType<ReadItemDto>(okResult.Value);
            Assert.Equal(updatedItem.id_item, returnedItem.id_item);
        }

        [Fact]
        public async Task DeleteItem_ReturnsNoContentResult()
        {
            // Arrange
            var itemId = 1;
            _mockItemService.Setup(service => service.DeleteItem(itemId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteItem(itemId);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }
    }
}