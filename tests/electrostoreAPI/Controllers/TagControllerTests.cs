using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using electrostore.Controllers;
using electrostore.Dto;
using electrostore.Services.TagService;

namespace electrostore.Tests.Controllers
{
    public class TagControllerTests
    {
        private readonly Mock<ITagService> _service;
        private readonly TagController _controller;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<HttpResponse> _mockHttpResponse;
        private readonly HeaderDictionary _headers;

        public TagControllerTests()
        {
            _service = new Mock<ITagService>();

            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpResponse = new Mock<HttpResponse>();
            _headers = new HeaderDictionary();
            _mockHttpResponse.Setup(r => r.Headers).Returns(_headers);
            _mockHttpContext.Setup(c => c.Response).Returns(_mockHttpResponse.Object);

            _controller = new TagController(_service.Object)
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
            var list = new List<ReadExtendedTagDto>
            {
                new() { id_tag = 1, nom_tag = "A", poids_tag = 1, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow },
                new() { id_tag = 2, nom_tag = "B", poids_tag = 1, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow }
            };
            _service.Setup(s => s.GetTags(100, 0, null, null)).ReturnsAsync(list);
            _service.Setup(s => s.GetTagsCount()).ReturnsAsync(list.Count);

            var res = await _controller.GetTags();

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsAssignableFrom<IEnumerable<ReadExtendedTagDto>>(ok.Value);
            Assert.Equal(2, value.Count());
            Assert.Equal("2", _headers["X-Total-Count"].ToString());
        }

        [Fact]
        public async Task GetById_ReturnsOk_WithTag()
        {
            var dto = new ReadExtendedTagDto { id_tag = 10, nom_tag = "T", poids_tag = 1, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.GetTagById(10, It.IsAny<List<string>>())).ReturnsAsync(dto);

            var res = await _controller.GetTagById(10);

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadExtendedTagDto>(ok.Value);
            Assert.Equal(10, value.id_tag);
        }

        [Fact]
        public async Task Create_ReturnsCreated_WithTag()
        {
            var created = new ReadTagDto { id_tag = 3, nom_tag = "N", poids_tag = 1, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.CreateTag(It.IsAny<CreateTagDto>())).ReturnsAsync(created);

            var res = await _controller.CreateTag(new CreateTagDto { nom_tag = "N", poids_tag = 1 });

            var createdRes = Assert.IsType<CreatedAtActionResult>(res.Result);
            var value = Assert.IsType<ReadTagDto>(createdRes.Value);
            Assert.Equal(3, value.id_tag);
        }

        [Fact]
        public async Task CreateBulk_ReturnsOk_WithBulk()
        {
            var bulk = new ReadBulkTagDto
            {
                Valide = new List<ReadTagDto> { new() { id_tag = 1, nom_tag = "A", poids_tag = 1, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow } },
                Error = new List<ErrorDetail>()
            };
            _service.Setup(s => s.CreateBulkTag(It.IsAny<List<CreateTagDto>>())).ReturnsAsync(bulk);

            var res = await _controller.CreateBulkTag(new List<CreateTagDto> { new() { nom_tag = "A", poids_tag = 1 } });

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadBulkTagDto>(ok.Value);
            Assert.Single(value.Valide);
        }

        [Fact]
        public async Task Update_ReturnsOk_WithUpdated()
        {
            var updated = new ReadTagDto { id_tag = 7, nom_tag = "U", poids_tag = 1, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.UpdateTag(7, It.IsAny<UpdateTagDto>())).ReturnsAsync(updated);

            var res = await _controller.UpdateTag(7, new UpdateTagDto { nom_tag = "U", poids_tag = 1 });

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadTagDto>(ok.Value);
            Assert.Equal(7, value.id_tag);
        }

        [Fact]
        public async Task Delete_ReturnsNoContent()
        {
            _service.Setup(s => s.DeleteTag(9)).Returns(Task.CompletedTask);
            var res = await _controller.DeleteTag(9);
            Assert.IsType<NoContentResult>(res);
        }
    }
}
