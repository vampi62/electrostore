using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using electrostore.Controllers;
using electrostore.Dto;
using electrostore.Services.ProjetProjetTagService;

namespace electrostore.Tests.Controllers
{
    public class ProjetProjetTagControllerTests
    {
        private readonly Mock<IProjetProjetTagService> _service;
        private readonly ProjetProjetTagController _controller;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<HttpResponse> _mockHttpResponse;
        private readonly HeaderDictionary _headers;

        public ProjetProjetTagControllerTests()
        {
            _service = new Mock<IProjetProjetTagService>();

            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpResponse = new Mock<HttpResponse>();
            _headers = new HeaderDictionary();
            _mockHttpResponse.Setup(r => r.Headers).Returns(_headers);
            _mockHttpContext.Setup(c => c.Response).Returns(_mockHttpResponse.Object);

            _controller = new ProjetProjetTagController(_service.Object)
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
            var list = new List<ReadExtendedProjetProjetTagDto>
            {
                new() { id_projet = 1, id_projet_tag = 10, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow },
                new() { id_projet = 1, id_projet_tag = 11, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow }
            };
            _service.Setup(s => s.GetProjetsProjetTagsByProjetId(1, 100, 0, null)).ReturnsAsync(list);
            _service.Setup(s => s.GetProjetsProjetTagsCountByProjetId(1)).ReturnsAsync(list.Count);

            var res = await _controller.GetProjetsProjetTagsByProjetId(1);

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsAssignableFrom<IEnumerable<ReadExtendedProjetProjetTagDto>>(ok.Value);
            Assert.Equal(2, value.Count());
            Assert.Equal("2", _headers["X-Total-Count"].ToString());
        }

        [Fact]
        public async Task GetById_ReturnsOk_WithLink()
        {
            var dto = new ReadExtendedProjetProjetTagDto { id_projet = 2, id_projet_tag = 20, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.GetProjetProjetTagById(2, 20, It.IsAny<List<string>>())).ReturnsAsync(dto);

            var res = await _controller.GetProjetProjetTagById(2, 20);

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadExtendedProjetProjetTagDto>(ok.Value);
            Assert.Equal(2, value.id_projet);
            Assert.Equal(20, value.id_projet_tag);
        }

        [Fact]
        public async Task Create_ReturnsCreated_WithCreatedLink()
        {
            var created = new ReadProjetProjetTagDto { id_projet = 3, id_projet_tag = 30, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.CreateProjetProjetTag(It.IsAny<CreateProjetProjetTagDto>())).ReturnsAsync(created);

            var res = await _controller.CreateProjetProjetTag(3, new CreateProjetProjetTagByProjetDto { id_projet_tag = 30 });

            var createdRes = Assert.IsType<CreatedAtActionResult>(res.Result);
            var value = Assert.IsType<ReadProjetProjetTagDto>(createdRes.Value);
            Assert.Equal(3, value.id_projet);
            Assert.Equal(30, value.id_projet_tag);
        }

        [Fact]
        public async Task CreateBulk_ReturnsOk_WithBulkResult()
        {
            var bulk = new ReadBulkProjetProjetTagDto
            {
                Valide = new List<ReadProjetProjetTagDto> { new() { id_projet = 4, id_projet_tag = 40, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow } },
                Error = new List<ErrorDetail>()
            };
            _service.Setup(s => s.CreateBulkProjetProjetTag(It.IsAny<List<CreateProjetProjetTagDto>>())).ReturnsAsync(bulk);

            var res = await _controller.CreateBulkProjetProjetTag(4, new List<CreateProjetProjetTagByProjetDto>
            {
                new() { id_projet_tag = 40 }
            });

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadBulkProjetProjetTagDto>(ok.Value);
            Assert.Single(value.Valide);
        }

        [Fact]
        public async Task Delete_ReturnsNoContent()
        {
            _service.Setup(s => s.DeleteProjetProjetTag(5, 50)).Returns(Task.CompletedTask);

            var res = await _controller.DeleteProjetProjetTag(5, 50);
            Assert.IsType<NoContentResult>(res);
        }

        [Fact]
        public async Task DeleteBulk_ReturnsOk_WithBulkResult()
        {
            var bulk = new ReadBulkProjetProjetTagDto
            {
                Valide = new List<ReadProjetProjetTagDto> { new() { id_projet = 6, id_projet_tag = 60, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow } },
                Error = new List<ErrorDetail>()
            };
            _service.Setup(s => s.DeleteBulkProjetProjetTag(It.IsAny<List<CreateProjetProjetTagDto>>())).ReturnsAsync(bulk);

            var res = await _controller.DeleteBulkProjetProjetTag(6, new List<int> { 60 });

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadBulkProjetProjetTagDto>(ok.Value);
            Assert.Single(value.Valide);
        }
    }
}
