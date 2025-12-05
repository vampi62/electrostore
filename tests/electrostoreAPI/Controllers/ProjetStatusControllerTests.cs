using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using electrostore.Controllers;
using electrostore.Dto;
using electrostore.Services.ProjetStatusService;

namespace electrostore.Tests.Controllers
{
    public class ProjetStatusControllerTests
    {
        private readonly Mock<IProjetStatusService> _service;
        private readonly ProjetStatusController _controller;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<HttpResponse> _mockHttpResponse;
        private readonly HeaderDictionary _headers;

        public ProjetStatusControllerTests()
        {
            _service = new Mock<IProjetStatusService>();

            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpResponse = new Mock<HttpResponse>();
            _headers = new HeaderDictionary();
            _mockHttpResponse.Setup(r => r.Headers).Returns(_headers);
            _mockHttpContext.Setup(c => c.Response).Returns(_mockHttpResponse.Object);

            _controller = new ProjetStatusController(_service.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = _mockHttpContext.Object
                }
            };
        }

        [Fact]
        public async Task GetByProjet_ReturnsOk_WithHeader()
        {
            var list = new List<ReadExtendedProjetStatusDto>
            {
                new() { id_projet_status = 1, id_projet = 1, status_projet = electrostore.Enums.ProjetStatus.NotStarted, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow },
                new() { id_projet_status = 2, id_projet = 1, status_projet = electrostore.Enums.ProjetStatus.InProgress, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow }
            };
            _service.Setup(s => s.GetProjetStatusByProjetId(1, 100, 0, null)).ReturnsAsync(list);
            _service.Setup(s => s.GetProjetStatusCountByProjetId(1)).ReturnsAsync(list.Count);

            var res = await _controller.GetProjetStatusByProjetId(1);

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsAssignableFrom<IEnumerable<ReadExtendedProjetStatusDto>>(ok.Value);
            Assert.Equal(2, value.Count());
            Assert.Equal("2", _headers["X-Total-Count"].ToString());
        }

        [Fact]
        public async Task GetById_ReturnsOk_WithStatus()
        {
            var dto = new ReadExtendedProjetStatusDto { id_projet_status = 10, id_projet = 2, status_projet = electrostore.Enums.ProjetStatus.Completed, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.GetProjetStatusById(10, null, 2, It.IsAny<List<string>>())).ReturnsAsync(dto);

            var res = await _controller.GetProjetStatusById(2, 10);

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadExtendedProjetStatusDto>(ok.Value);
            Assert.Equal(10, value.id_projet_status);
        }
    }
}
