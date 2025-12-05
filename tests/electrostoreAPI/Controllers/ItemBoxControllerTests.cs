using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using electrostore.Controllers;
using electrostore.Dto;
using electrostore.Services.ItemBoxService;

namespace electrostore.Tests.Controllers
{
    public class ItemBoxControllerTests
    {
        private readonly Mock<IItemBoxService> _service;
        private readonly ItemBoxController _controller;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<HttpResponse> _mockHttpResponse;
        private readonly HeaderDictionary _headers;

        public ItemBoxControllerTests()
        {
            _service = new Mock<IItemBoxService>();

            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpResponse = new Mock<HttpResponse>();
            _headers = new HeaderDictionary();
            _mockHttpResponse.Setup(r => r.Headers).Returns(_headers);
            _mockHttpContext.Setup(c => c.Response).Returns(_mockHttpResponse.Object);

            _controller = new ItemBoxController(_service.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = _mockHttpContext.Object
                }
            };
        }

        [Fact]
        public async Task GetByItem_ReturnsOk_WithList_AndHeader()
        {
            var list = new List<ReadExtendedItemBoxDto>
            {
                new() { id_item = 1, id_box = 10, qte_item_box = 2, seuil_max_item_item_box = 20, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow },
                new() { id_item = 1, id_box = 11, qte_item_box = 3, seuil_max_item_item_box = 30, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow }
            };
            _service.Setup(s => s.GetItemsBoxsByItemId(1, 100, 0, null)).ReturnsAsync(list);
            _service.Setup(s => s.GetItemsBoxsCountByItemId(1)).ReturnsAsync(list.Count);

            var res = await _controller.GetItemsBoxsByItemId(1);

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsAssignableFrom<IEnumerable<ReadExtendedItemBoxDto>>(ok.Value);
            Assert.Equal(2, value.Count());
            Assert.Equal("2", _headers["X-Total-Count"].ToString());
        }

        [Fact]
        public async Task GetById_ReturnsOk_WithItemBox()
        {
            var dto = new ReadExtendedItemBoxDto { id_item = 2, id_box = 20, qte_item_box = 5, seuil_max_item_item_box = 50, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.GetItemBoxById(2, 20, It.IsAny<List<string>>())).ReturnsAsync(dto);

            var res = await _controller.GetItemBoxById(2, 20);

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadExtendedItemBoxDto>(ok.Value);
            Assert.Equal(2, value.id_item);
            Assert.Equal(20, value.id_box);
        }

        [Fact]
        public async Task Create_ReturnsCreated_WithCreatedItemBox()
        {
            var created = new ReadItemBoxDto { id_item = 3, id_box = 30, qte_item_box = 9, seuil_max_item_item_box = 90, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.CreateItemBox(It.IsAny<CreateItemBoxDto>())).ReturnsAsync(created);

            var res = await _controller.CreateItemBox(3, new CreateItemBoxByItemDto { id_box = 30, qte_item_box = 9, seuil_max_item_item_box = 90 });

            var createdRes = Assert.IsType<CreatedAtActionResult>(res.Result);
            var value = Assert.IsType<ReadItemBoxDto>(createdRes.Value);
            Assert.Equal(3, value.id_item);
            Assert.Equal(30, value.id_box);
        }

        [Fact]
        public async Task Update_ReturnsOk_WithUpdatedItemBox()
        {
            var updated = new ReadItemBoxDto { id_item = 4, id_box = 40, qte_item_box = 1, seuil_max_item_item_box = 10, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.UpdateItemBox(4, 40, It.IsAny<UpdateItemBoxDto>())).ReturnsAsync(updated);

            var res = await _controller.UpdateItemBox(4, 40, new UpdateItemBoxDto { qte_item_box = 1, seuil_max_item_item_box = 10 });

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadItemBoxDto>(ok.Value);
            Assert.Equal(4, value.id_item);
            Assert.Equal(40, value.id_box);
        }

        [Fact]
        public async Task Delete_ReturnsNoContent()
        {
            _service.Setup(s => s.DeleteItemBox(5, 50)).Returns(Task.CompletedTask);

            var res = await _controller.DeleteItemBox(5, 50);
            Assert.IsType<NoContentResult>(res);
        }
    }
}
