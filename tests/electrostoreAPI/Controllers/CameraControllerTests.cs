using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using electrostore.Controllers;
using electrostore.Dto;
using electrostore.Services.CameraService;

namespace electrostore.Tests.Controllers
{
    public class CameraControllerTests
    {
        private readonly Mock<ICameraService> _service;
        private readonly CameraController _controller;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<HttpResponse> _mockHttpResponse;
        private readonly HeaderDictionary _headers;

        public CameraControllerTests()
        {
            _service = new Mock<ICameraService>();

            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpResponse = new Mock<HttpResponse>();
            _headers = new HeaderDictionary();
            _mockHttpResponse.Setup(r => r.Headers).Returns(_headers);
            _mockHttpContext.Setup(c => c.Response).Returns(_mockHttpResponse.Object);

            _controller = new CameraController(_service.Object)
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
            var list = new List<ReadCameraDto>
            {
                new() { id_camera = 1, nom_camera = "Cam1", url_camera = "http://x", user_camera = null, mdp_camera = null, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow },
                new() { id_camera = 2, nom_camera = "Cam2", url_camera = "http://y", user_camera = null, mdp_camera = null, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow }
            };
            _service.Setup(s => s.GetCameras(100, 0, null)).ReturnsAsync(list);
            _service.Setup(s => s.GetCamerasCount()).ReturnsAsync(list.Count);

            var res = await _controller.GetCameras();
            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsAssignableFrom<IEnumerable<ReadCameraDto>>(ok.Value);
            Assert.Equal(2, value.Count());
            Assert.Equal("2", _headers["X-Total-Count"].ToString());
        }

        [Fact]
        public async Task GetById_ReturnsOk_WithCamera()
        {
            var dto = new ReadCameraDto { id_camera = 1, nom_camera = "Cam1", url_camera = "http://x", user_camera = null, mdp_camera = null, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.GetCameraById(1)).ReturnsAsync(dto);

            var res = await _controller.GetCameraById(1);
            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadCameraDto>(ok.Value);
            Assert.Equal(1, value.id_camera);
        }
    }
}
