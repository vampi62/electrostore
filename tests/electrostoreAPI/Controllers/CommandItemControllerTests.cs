using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using electrostore.Controllers;
using electrostore.Dto;
using electrostore.Services.CommandItemService;

namespace electrostore.Tests.Controllers
{
    public class CommandItemControllerTests
    {
        private readonly Mock<ICommandItemService> _service;
        private readonly CommandItemController _controller;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<HttpResponse> _mockHttpResponse;
        private readonly HeaderDictionary _headers;

        public CommandItemControllerTests()
        {
            _service = new Mock<ICommandItemService>();

            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpResponse = new Mock<HttpResponse>();
            _headers = new HeaderDictionary();
            _mockHttpResponse.Setup(r => r.Headers).Returns(_headers);
            _mockHttpContext.Setup(c => c.Response).Returns(_mockHttpResponse.Object);

            _controller = new CommandItemController(_service.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = _mockHttpContext.Object
                }
            };
        }

        [Fact]
        public async Task GetByCommand_ReturnsOk_WithList_AndHeader()
        {
            var list = new List<ReadExtendedCommandItemDto>
            {
                new() { id_command = 1, id_item = 10, qte_command_item = 2, prix_command_item = 3.3f, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow },
                new() { id_command = 1, id_item = 11, qte_command_item = 1, prix_command_item = 1.0f, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow }
            };
            _service.Setup(s => s.GetCommandsItemsByCommandId(1, 100, 0, null)).ReturnsAsync(list);
            _service.Setup(s => s.GetCommandsItemsCountByCommandId(1)).ReturnsAsync(list.Count);

            var res = await _controller.GetCommandsItemsByCommandId(1);

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsAssignableFrom<IEnumerable<ReadExtendedCommandItemDto>>(ok.Value);
            Assert.Equal(2, value.Count());
            Assert.Equal("2", _headers["X-Total-Count"].ToString());
        }

        [Fact]
        public async Task GetById_ReturnsOk_WithItem()
        {
            var dto = new ReadExtendedCommandItemDto { id_command = 2, id_item = 20, qte_command_item = 4, prix_command_item = 7.7f, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.GetCommandItemById(2, 20, It.IsAny<List<string>>())).ReturnsAsync(dto);

            var res = await _controller.GetCommandItemById(2, 20);

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadExtendedCommandItemDto>(ok.Value);
            Assert.Equal(2, value.id_command);
            Assert.Equal(20, value.id_item);
        }

        [Fact]
        public async Task Create_ReturnsCreated_WithCreatedItem()
        {
            var created = new ReadCommandItemDto { id_command = 3, id_item = 30, qte_command_item = 5, prix_command_item = 9.9f, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.CreateCommandItem(It.IsAny<CreateCommandItemDto>())).ReturnsAsync(created);

            var res = await _controller.CreateCommandItem(3, new CreateCommandItemByCommandDto { id_item = 30, qte_command_item = 5, prix_command_item = 9.9f });

            var createdRes = Assert.IsType<CreatedAtActionResult>(res.Result);
            var value = Assert.IsType<ReadCommandItemDto>(createdRes.Value);
            Assert.Equal(3, value.id_command);
            Assert.Equal(30, value.id_item);
        }

        [Fact]
        public async Task CreateBulk_ReturnsOk_WithBulkResult()
        {
            var bulk = new ReadBulkCommandItemDto
            {
                Valide = new List<ReadCommandItemDto> { new() { id_command = 4, id_item = 40, qte_command_item = 1, prix_command_item = 1.1f, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow } },
                Error = new List<ErrorDetail>()
            };
            _service.Setup(s => s.CreateBulkCommandItem(It.IsAny<List<CreateCommandItemDto>>())).ReturnsAsync(bulk);

            var res = await _controller.CreateBulkCommandItem(4, new List<CreateCommandItemByCommandDto>
            {
                new() { id_item = 40, qte_command_item = 1, prix_command_item = 1.1f }
            });

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadBulkCommandItemDto>(ok.Value);
            Assert.Single(value.Valide);
        }

        [Fact]
        public async Task Update_ReturnsOk_WithUpdatedItem()
        {
            var updated = new ReadCommandItemDto { id_command = 5, id_item = 50, qte_command_item = 3, prix_command_item = 2.2f, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.UpdateCommandItem(5, 50, It.IsAny<UpdateCommandItemDto>())).ReturnsAsync(updated);

            var res = await _controller.UpdateCommandItem(5, 50, new UpdateCommandItemDto { qte_command_item = 3, prix_command_item = 2.2f });

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadCommandItemDto>(ok.Value);
            Assert.Equal(50, value.id_item);
        }

        [Fact]
        public async Task Delete_ReturnsNoContent()
        {
            _service.Setup(s => s.DeleteCommandItem(6, 60)).Returns(Task.CompletedTask);

            var res = await _controller.DeleteCommandItem(6, 60);
            Assert.IsType<NoContentResult>(res);
        }
    }
}
