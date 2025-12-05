using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using electrostore.Controllers;
using electrostore.Dto;
using electrostore.Services.ProjetTagService;

namespace electrostore.Tests.Controllers
{
    public class ProjetTagControllerTests
    {
        private readonly Mock<IProjetTagService> _service;
        private readonly ProjetTagController _controller;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<HttpResponse> _mockHttpResponse;
        private readonly HeaderDictionary _headers;

        public ProjetTagControllerTests()
        {
            _service = new Mock<IProjetTagService>();

            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpResponse = new Mock<HttpResponse>();
            _headers = new HeaderDictionary();
            _mockHttpResponse.Setup(r => r.Headers).Returns(_headers);
            _mockHttpContext.Setup(c => c.Response).Returns(_mockHttpResponse.Object);

            _controller = new ProjetTagController(_service.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = _mockHttpContext.Object
                }
            };
        }

        [Fact]
        public async Task GetList_ReturnsOk_WithHeader()
        {
            var list = new List<ReadExtendedProjetTagDto>
            {
                new() { id_projet_tag = 1, nom_projet_tag = "A", poids_projet_tag = 1, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow },
                new() { id_projet_tag = 2, nom_projet_tag = "B", poids_projet_tag = 2, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow }
            };
            _service.Setup(s => s.GetProjetTags(100, 0, null, null)).ReturnsAsync(list);
            _service.Setup(s => s.GetProjetTagsCount()).ReturnsAsync(list.Count);

            var res = await _controller.GetProjetTags();

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsAssignableFrom<IEnumerable<ReadExtendedProjetTagDto>>(ok.Value);
            Assert.Equal(2, value.Count());
            Assert.Equal("2", _headers["X-Total-Count"].ToString());
        }

        [Fact]
        public async Task GetById_ReturnsOk_WithTag()
        {
            var dto = new ReadExtendedProjetTagDto { id_projet_tag = 10, nom_projet_tag = "PT", poids_projet_tag = 0, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.GetProjetTagById(10, It.IsAny<List<string>>())).ReturnsAsync(dto);

            var res = await _controller.GetProjetTagById(10);

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadExtendedProjetTagDto>(ok.Value);
            Assert.Equal(10, value.id_projet_tag);
        }

        [Fact]
        public async Task Create_ReturnsCreated_WithTag()
        {
            var created = new ReadProjetTagDto { id_projet_tag = 3, nom_projet_tag = "New", poids_projet_tag = 5, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.CreateProjetTag(It.IsAny<CreateProjetTagDto>())).ReturnsAsync(created);

            var res = await _controller.CreateProjetTag(new CreateProjetTagDto { nom_projet_tag = "New", poids_projet_tag = 5 });

            var createdRes = Assert.IsType<CreatedAtActionResult>(res.Result);
            var value = Assert.IsType<ReadProjetTagDto>(createdRes.Value);
            Assert.Equal(3, value.id_projet_tag);
            Assert.Equal("New", value.nom_projet_tag);
        }

        [Fact]
        public async Task CreateBulk_ReturnsOk_WithBulk()
        {
            var bulk = new ReadBulkProjetTagDto
            {
                Valide = new List<ReadProjetTagDto> { new() { id_projet_tag = 5, nom_projet_tag = "A", poids_projet_tag = 1, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow } },
                Error = new List<ErrorDetail>()
            };
            _service.Setup(s => s.CreateBulkProjetTag(It.IsAny<List<CreateProjetTagDto>>())).ReturnsAsync(bulk);

            var res = await _controller.CreateBulkProjetTag(new List<CreateProjetTagDto> { new() { nom_projet_tag = "A", poids_projet_tag = 1 } });

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadBulkProjetTagDto>(ok.Value);
            Assert.Single(value.Valide);
        }

        [Fact]
        public async Task Update_ReturnsOk_WithUpdated()
        {
            var updated = new ReadProjetTagDto { id_projet_tag = 7, nom_projet_tag = "Upd", poids_projet_tag = 10, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.UpdateProjetTag(7, It.IsAny<UpdateProjetTagDto>())).ReturnsAsync(updated);

            var res = await _controller.UpdateProjetTag(7, new UpdateProjetTagDto { nom_projet_tag = "Upd", poids_projet_tag = 10 });

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadProjetTagDto>(ok.Value);
            Assert.Equal(7, value.id_projet_tag);
        }

        [Fact]
        public async Task Delete_ReturnsNoContent()
        {
            _service.Setup(s => s.DeleteProjetTag(9)).Returns(Task.CompletedTask);
            var res = await _controller.DeleteProjetTag(9);
            Assert.IsType<NoContentResult>(res);
        }
    }
}
