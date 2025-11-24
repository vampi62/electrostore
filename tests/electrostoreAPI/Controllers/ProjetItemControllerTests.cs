using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using electrostore.Controllers;
using electrostore.Dto;
using electrostore.Services.ProjetItemService;

namespace electrostore.Tests.Controllers
{
    public class ProjetItemControllerTests
    {
        private readonly Mock<IProjetItemService> _service;
        private readonly ProjetItemController _controller;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<HttpResponse> _mockHttpResponse;
        private readonly HeaderDictionary _headers;

        public ProjetItemControllerTests()
        {
            _service = new Mock<IProjetItemService>();

            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpResponse = new Mock<HttpResponse>();
            _headers = new HeaderDictionary();
            _mockHttpResponse.Setup(r => r.Headers).Returns(_headers);
            _mockHttpContext.Setup(c => c.Response).Returns(_mockHttpResponse.Object);

            _controller = new ProjetItemController(_service.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = _mockHttpContext.Object
                }
            };
        }

        [Fact]
        public async Task GetByProjet_ReturnsOk_WithList_AndHeader()
        {
            var list = new List<ReadExtendedProjetItemDto>
            {
                new() { id_projet = 1, id_item = 10, qte_projet_item = 2, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow },
                new() { id_projet = 1, id_item = 11, qte_projet_item = 3, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow }
            };
            _service.Setup(s => s.GetProjetItemsByProjetId(1, 100, 0, null)).ReturnsAsync(list);
            _service.Setup(s => s.GetProjetItemsCountByProjetId(1)).ReturnsAsync(list.Count);

            var res = await _controller.GetProjetItemsByProjetId(1);

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsAssignableFrom<IEnumerable<ReadExtendedProjetItemDto>>(ok.Value);
            Assert.Equal(2, value.Count());
            Assert.Equal("2", _headers["X-Total-Count"].ToString());
        }

        [Fact]
        public async Task GetById_ReturnsOk_WithProjetItem()
        {
            var dto = new ReadExtendedProjetItemDto { id_projet = 2, id_item = 20, qte_projet_item = 4, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.GetProjetItemById(2, 20, It.IsAny<List<string>>())).ReturnsAsync(dto);

            var res = await _controller.GetProjetItemById(2, 20);

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadExtendedProjetItemDto>(ok.Value);
            Assert.Equal(2, value.id_projet);
            Assert.Equal(20, value.id_item);
        }

        [Fact]
        public async Task Create_ReturnsCreated_WithCreatedProjetItem()
        {
            var created = new ReadProjetItemDto { id_projet = 3, id_item = 30, qte_projet_item = 5, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.CreateProjetItem(It.IsAny<CreateProjetItemDto>())).ReturnsAsync(created);

            var res = await _controller.CreateProjetItem(3, new CreateProjetItemByProjetDto { id_item = 30, qte_projet_item = 5 });

            var createdRes = Assert.IsType<CreatedAtActionResult>(res.Result);
            var value = Assert.IsType<ReadProjetItemDto>(createdRes.Value);
            Assert.Equal(3, value.id_projet);
            Assert.Equal(30, value.id_item);
        }

        [Fact]
        public async Task CreateBulk_ReturnsOk_WithBulkResult()
        {
            var bulk = new ReadBulkProjetItemDto
            {
                Valide = new List<ReadProjetItemDto> { new() { id_projet = 4, id_item = 40, qte_projet_item = 1, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow } },
                Error = new List<ErrorDetail>()
            };
            _service.Setup(s => s.CreateBulkProjetItem(It.IsAny<List<CreateProjetItemDto>>())).ReturnsAsync(bulk);

            var res = await _controller.CreateBulkProjetItem(4, new List<CreateProjetItemByProjetDto>
            {
                new() { id_item = 40, qte_projet_item = 1 }
            });

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadBulkProjetItemDto>(ok.Value);
            Assert.Single(value.Valide);
        }

        [Fact]
        public async Task Update_ReturnsOk_WithUpdatedProjetItem()
        {
            var updated = new ReadProjetItemDto { id_projet = 5, id_item = 50, qte_projet_item = 3, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.UpdateProjetItem(5, 50, It.IsAny<UpdateProjetItemDto>())).ReturnsAsync(updated);

            var res = await _controller.UpdateProjetItem(5, 50, new UpdateProjetItemDto { qte_projet_item = 3 });

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadProjetItemDto>(ok.Value);
            Assert.Equal(50, value.id_item);
        }

        [Fact]
        public async Task Delete_ReturnsNoContent()
        {
            _service.Setup(s => s.DeleteProjetItem(6, 60)).Returns(Task.CompletedTask);

            var res = await _controller.DeleteProjetItem(6, 60);
            Assert.IsType<NoContentResult>(res);
        }
    }
}
