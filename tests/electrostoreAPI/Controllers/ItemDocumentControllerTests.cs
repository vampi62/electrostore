using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using electrostore.Controllers;
using electrostore.Dto;
using electrostore.Services.ItemDocumentService;
using electrostore.Services.FileService;

namespace electrostore.Tests.Controllers
{
    public class ItemDocumentControllerTests
    {
        private readonly Mock<IItemDocumentService> _docService;
        private readonly Mock<IFileService> _fileService;
        private readonly ItemDocumentController _controller;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<HttpResponse> _mockHttpResponse;
        private readonly HeaderDictionary _headers;

        public ItemDocumentControllerTests()
        {
            _docService = new Mock<IItemDocumentService>();
            _fileService = new Mock<IFileService>();

            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpResponse = new Mock<HttpResponse>();
            _headers = new HeaderDictionary();
            _mockHttpResponse.Setup(r => r.Headers).Returns(_headers);
            _mockHttpContext.Setup(c => c.Response).Returns(_mockHttpResponse.Object);

            _controller = new ItemDocumentController(_docService.Object, _fileService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = _mockHttpContext.Object
                }
            };
        }

        [Fact]
        public async Task GetByItem_ReturnsOk_WithHeader()
        {
            var list = new List<ReadItemDocumentDto>
            {
                new() { id_item_document = 1, id_item = 1, url_item_document = "/a.pdf", name_item_document = "a", type_item_document = "application/pdf", size_item_document = 1, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow },
                new() { id_item_document = 2, id_item = 1, url_item_document = "/b.pdf", name_item_document = "b", type_item_document = "application/pdf", size_item_document = 1, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow }
            };
            _docService.Setup(s => s.GetItemsDocumentsByItemId(1, 100, 0)).ReturnsAsync(list);
            _docService.Setup(s => s.GetItemsDocumentsCountByItemId(1)).ReturnsAsync(list.Count);

            var res = await _controller.GetItemsDocumentsByItemId(1, 100, 0);

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsAssignableFrom<IEnumerable<ReadItemDocumentDto>>(ok.Value);
            Assert.Equal(2, value.Count());
            Assert.Equal("2", _headers["X-Total-Count"].ToString());
        }

        [Fact]
        public async Task GetById_ReturnsOk_WithDoc()
        {
            var dto = new ReadItemDocumentDto { id_item_document = 5, id_item = 2, url_item_document = "/c.pdf", name_item_document = "c", type_item_document = "application/pdf", size_item_document = 10, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _docService.Setup(s => s.GetItemDocumentById(5, 2)).ReturnsAsync(dto);

            var res = await _controller.GetItemDocumentById(5, 2);

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadItemDocumentDto>(ok.Value);
            Assert.Equal(5, value.id_item_document);
        }

        [Fact]
        public async Task Download_ReturnsFile_WhenSuccess()
        {
            var dto = new ReadItemDocumentDto { id_item_document = 7, id_item = 3, url_item_document = "/d.pdf", name_item_document = "d.pdf", type_item_document = "application/pdf", size_item_document = 12, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _docService.Setup(s => s.GetItemDocumentById(7, 3)).ReturnsAsync(dto);

            var ms = new MemoryStream(new byte[] { 1, 2, 3 });
            _fileService.Setup(f => f.GetFile("/d.pdf")).ReturnsAsync(new GetFileResult { FileStream = ms, MimeType = "application/pdf", Success = true, ErrorMessage = null });

            var res = await _controller.DownloadItemDocument(7, 3);
            Assert.IsType<FileStreamResult>(res);
        }

        [Fact]
        public async Task Update_ReturnsOk()
        {
            var updated = new ReadItemDocumentDto { id_item_document = 9, id_item = 4, url_item_document = "/x.pdf", name_item_document = "x", type_item_document = "application/pdf", size_item_document = 1, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _docService.Setup(s => s.UpdateItemDocument(9, It.IsAny<UpdateItemDocumentDto>(), 4)).ReturnsAsync(updated);

            var res = await _controller.UpdateItemDocument(9, new UpdateItemDocumentDto { name_item_document = "x" }, 4);
            var ok = Assert.IsType<OkObjectResult>(res.Result);
            Assert.IsType<ReadItemDocumentDto>(ok.Value);
        }

        [Fact]
        public async Task Delete_ReturnsNoContent()
        {
            _docService.Setup(s => s.DeleteItemDocument(11, 5)).Returns(Task.CompletedTask);
            var res = await _controller.DeleteItemDocument(11, 5);
            Assert.IsType<NoContentResult>(res);
        }
    }
}
