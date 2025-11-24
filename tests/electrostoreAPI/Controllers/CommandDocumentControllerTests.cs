using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using electrostore.Controllers;
using electrostore.Dto;
using electrostore.Services.CommandDocumentService;
using electrostore.Services.FileService;

namespace electrostore.Tests.Controllers
{
    public class CommandDocumentControllerTests
    {
        private readonly Mock<ICommandDocumentService> _docService;
        private readonly Mock<IFileService> _fileService;
        private readonly CommandDocumentController _controller;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<HttpResponse> _mockHttpResponse;
        private readonly HeaderDictionary _headers;

        public CommandDocumentControllerTests()
        {
            _docService = new Mock<ICommandDocumentService>();
            _fileService = new Mock<IFileService>();

            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpResponse = new Mock<HttpResponse>();
            _headers = new HeaderDictionary();
            _mockHttpResponse.Setup(r => r.Headers).Returns(_headers);
            _mockHttpContext.Setup(c => c.Response).Returns(_mockHttpResponse.Object);

            _controller = new CommandDocumentController(_docService.Object, _fileService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = _mockHttpContext.Object
                }
            };
        }

        [Fact]
        public async Task GetByCommand_ReturnsOk_WithHeader()
        {
            var list = new List<ReadCommandDocumentDto>
            {
                new() { id_command_document = 1, id_command = 1, url_command_document = "/a.pdf", name_command_document = "a", type_command_document = "application/pdf", size_command_document = 1, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow },
                new() { id_command_document = 2, id_command = 1, url_command_document = "/b.pdf", name_command_document = "b", type_command_document = "application/pdf", size_command_document = 1, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow }
            };
            _docService.Setup(s => s.GetCommandsDocumentsByCommandId(1, 100, 0)).ReturnsAsync(list);
            _docService.Setup(s => s.GetCommandsDocumentsCountByCommandId(1)).ReturnsAsync(list.Count);

            var res = await _controller.GetCommandsDocumentsByCommandId(1, 100, 0);

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsAssignableFrom<IEnumerable<ReadCommandDocumentDto>>(ok.Value);
            Assert.Equal(2, value.Count());
            Assert.Equal("2", _headers["X-Total-Count"].ToString());
        }

        [Fact]
        public async Task GetById_ReturnsOk_WithDoc()
        {
            var dto = new ReadCommandDocumentDto { id_command_document = 5, id_command = 2, url_command_document = "/c.pdf", name_command_document = "c", type_command_document = "application/pdf", size_command_document = 10, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _docService.Setup(s => s.GetCommandDocumentById(5, 2)).ReturnsAsync(dto);

            var res = await _controller.GetCommandDocumentById(5, 2);

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadCommandDocumentDto>(ok.Value);
            Assert.Equal(5, value.id_command_document);
        }

        [Fact]
        public async Task Download_ReturnsFile_WhenSuccess()
        {
            var dto = new ReadCommandDocumentDto { id_command_document = 7, id_command = 3, url_command_document = "/d.pdf", name_command_document = "d.pdf", type_command_document = "application/pdf", size_command_document = 12, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _docService.Setup(s => s.GetCommandDocumentById(7, 3)).ReturnsAsync(dto);

            var ms = new MemoryStream(new byte[] { 1, 2, 3 });
            _fileService.Setup(f => f.GetFile("/d.pdf")).ReturnsAsync(new GetFileResult { FileStream = ms, MimeType = "application/pdf", Success = true, ErrorMessage = null });

            var res = await _controller.DownloadCommandDocument(7, 3);
            Assert.IsType<FileStreamResult>(res);
        }

        [Fact]
        public async Task Update_ReturnsOk()
        {
            var updated = new ReadCommandDocumentDto { id_command_document = 9, id_command = 4, url_command_document = "/x.pdf", name_command_document = "x", type_command_document = "application/pdf", size_command_document = 1, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _docService.Setup(s => s.UpdateCommandDocument(9, It.IsAny<UpdateCommandDocumentDto>(), 4)).ReturnsAsync(updated);

            var res = await _controller.UpdateCommandDocument(9, new UpdateCommandDocumentDto { name_command_document = "x" }, 4);
            var ok = Assert.IsType<OkObjectResult>(res.Result);
            Assert.IsType<ReadCommandDocumentDto>(ok.Value);
        }

        [Fact]
        public async Task Delete_ReturnsNoContent()
        {
            _docService.Setup(s => s.DeleteCommandDocument(11, 5)).Returns(Task.CompletedTask);
            var res = await _controller.DeleteCommandDocument(11, 5);
            Assert.IsType<NoContentResult>(res);
        }
    }
}
