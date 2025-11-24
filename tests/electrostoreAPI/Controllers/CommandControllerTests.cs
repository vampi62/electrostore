using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using electrostore.Controllers;
using electrostore.Dto;
using electrostore.Services.CommandService;

namespace electrostore.Tests.Controllers
{
    public class CommandControllerTests
    {
        private readonly Mock<ICommandService> _service;
        private readonly CommandController _controller;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<HttpResponse> _mockHttpResponse;
        private readonly HeaderDictionary _headers;

        public CommandControllerTests()
        {
            _service = new Mock<ICommandService>();

            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpResponse = new Mock<HttpResponse>();
            _headers = new HeaderDictionary();
            _mockHttpResponse.Setup(r => r.Headers).Returns(_headers);
            _mockHttpContext.Setup(c => c.Response).Returns(_mockHttpResponse.Object);

            _controller = new CommandController(_service.Object)
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
            var list = new List<ReadExtendedCommandDto>
            {
                new() { id_command = 1, prix_command = 1, url_command = "http://a", status_command = "ok", date_command = DateTime.UtcNow, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow },
                new() { id_command = 2, prix_command = 2, url_command = "http://b", status_command = "ok", date_command = DateTime.UtcNow, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow }
            };
            _service.Setup(s => s.GetCommands(100, 0, null, null)).ReturnsAsync(list);
            _service.Setup(s => s.GetCommandsCount()).ReturnsAsync(list.Count);

            var res = await _controller.GetCommands();

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsAssignableFrom<IEnumerable<ReadExtendedCommandDto>>(ok.Value);
            Assert.Equal(2, value.Count());
            Assert.Equal("2", _headers["X-Total-Count"].ToString());
        }

        [Fact]
        public async Task GetById_ReturnsOk_WithCommand()
        {
            var dto = new ReadExtendedCommandDto { id_command = 10, prix_command = 3, url_command = "http://x", status_command = "ok", date_command = DateTime.UtcNow, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.GetCommandById(10, It.IsAny<List<string>>())).ReturnsAsync(dto);

            var res = await _controller.GetCommandById(10);

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadExtendedCommandDto>(ok.Value);
            Assert.Equal(10, value.id_command);
        }

        [Fact]
        public async Task Create_ReturnsCreated_WithCommand()
        {
            var created = new ReadCommandDto { id_command = 3, prix_command = 9, url_command = "http://c", status_command = "ok", date_command = DateTime.UtcNow, date_livraison_command = null, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.CreateCommand(It.IsAny<CreateCommandDto>())).ReturnsAsync(created);

            var res = await _controller.CreateCommand(new CreateCommandDto { prix_command = 9, url_command = "http://c", status_command = "ok", date_command = DateTime.UtcNow });

            var createdRes = Assert.IsType<CreatedAtActionResult>(res.Result);
            var value = Assert.IsType<ReadCommandDto>(createdRes.Value);
            Assert.Equal(3, value.id_command);
        }

        [Fact]
        public async Task Update_ReturnsOk_WithUpdated()
        {
            var updated = new ReadCommandDto { id_command = 4, prix_command = 5, url_command = "http://u", status_command = "ok", date_command = DateTime.UtcNow, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.UpdateCommand(4, It.IsAny<UpdateCommandDto>())).ReturnsAsync(updated);

            var res = await _controller.UpdateCommand(4, new UpdateCommandDto { prix_command = 5 });

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadCommandDto>(ok.Value);
            Assert.Equal(4, value.id_command);
        }

        [Fact]
        public async Task Delete_ReturnsNoContent()
        {
            _service.Setup(s => s.DeleteCommand(7)).Returns(Task.CompletedTask);
            var res = await _controller.DeleteCommand(7);
            Assert.IsType<NoContentResult>(res);
        }
    }
}
