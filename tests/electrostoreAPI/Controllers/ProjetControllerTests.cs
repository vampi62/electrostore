using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using electrostore.Controllers;
using electrostore.Dto;
using electrostore.Services.ProjetService;

namespace electrostore.Tests.Controllers
{
    public class ProjetControllerTests
    {
        private readonly Mock<IProjetService> _service;
        private readonly ProjetController _controller;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<HttpResponse> _mockHttpResponse;
        private readonly HeaderDictionary _headers;

        public ProjetControllerTests()
        {
            _service = new Mock<IProjetService>();

            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpResponse = new Mock<HttpResponse>();
            _headers = new HeaderDictionary();
            _mockHttpResponse.Setup(r => r.Headers).Returns(_headers);
            _mockHttpContext.Setup(c => c.Response).Returns(_mockHttpResponse.Object);

            _controller = new ProjetController(_service.Object)
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
            var list = new List<ReadExtendedProjetDto>
            {
                new() { id_projet = 1, nom_projet = "P1", description_projet = "D1", url_projet = "http://a", status_projet = electrostore.Enums.ProjetStatus.NotStarted, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow },
                new() { id_projet = 2, nom_projet = "P2", description_projet = "D2", url_projet = "http://b", status_projet = electrostore.Enums.ProjetStatus.NotStarted, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow }
            };
            _service.Setup(s => s.GetProjets(100, 0, null, null)).ReturnsAsync(list);
            _service.Setup(s => s.GetProjetsCount()).ReturnsAsync(list.Count);

            var res = await _controller.GetProjets();

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsAssignableFrom<IEnumerable<ReadExtendedProjetDto>>(ok.Value);
            Assert.Equal(2, value.Count());
            Assert.Equal("2", _headers["X-Total-Count"].ToString());
        }

        [Fact]
        public async Task GetById_ReturnsOk_WithProjet()
        {
            var dto = new ReadExtendedProjetDto { id_projet = 10, nom_projet = "PX", description_projet = "DX", url_projet = "http://x", status_projet = electrostore.Enums.ProjetStatus.InProgress, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.GetProjetById(10, It.IsAny<List<string>>())).ReturnsAsync(dto);

            var res = await _controller.GetProjetById(10);

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadExtendedProjetDto>(ok.Value);
            Assert.Equal(10, value.id_projet);
        }

        [Fact]
        public async Task Create_ReturnsCreated_WithProjet()
        {
            var created = new ReadProjetDto { id_projet = 3, nom_projet = "P3", description_projet = "D3", url_projet = "http://c", status_projet = electrostore.Enums.ProjetStatus.NotStarted, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.CreateProjet(It.IsAny<CreateProjetDto>())).ReturnsAsync(created);

            var res = await _controller.CreateProjet(new CreateProjetDto { nom_projet = "P3", description_projet = "D3", url_projet = "http://c", status_projet = electrostore.Enums.ProjetStatus.NotStarted });

            var createdRes = Assert.IsType<CreatedAtActionResult>(res.Result);
            var value = Assert.IsType<ReadProjetDto>(createdRes.Value);
            Assert.Equal(3, value.id_projet);
        }

        [Fact]
        public async Task Update_ReturnsOk_WithUpdated()
        {
            var updated = new ReadProjetDto { id_projet = 4, nom_projet = "P4", description_projet = "D4", url_projet = "http://u", status_projet = electrostore.Enums.ProjetStatus.NotStarted, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.UpdateProjet(4, It.IsAny<UpdateProjetDto>())).ReturnsAsync(updated);

            var res = await _controller.UpdateProjet(4, new UpdateProjetDto { nom_projet = "P4" });

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadProjetDto>(ok.Value);
            Assert.Equal(4, value.id_projet);
        }

        [Fact]
        public async Task Delete_ReturnsNoContent()
        {
            _service.Setup(s => s.DeleteProjet(7)).Returns(Task.CompletedTask);
            var res = await _controller.DeleteProjet(7);
            Assert.IsType<NoContentResult>(res);
        }
    }
}
