using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using electrostore.Controllers;
using electrostore.Dto;
using electrostore.Services.ProjetDocumentService;
using electrostore.Services.FileService;

namespace electrostore.Tests.Controllers
{
    public class ProjetDocumentControllerTests
    {
        private readonly Mock<IProjetDocumentService> _docService;
        private readonly Mock<IFileService> _fileService;
        private readonly ProjetDocumentController _controller;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<HttpResponse> _mockHttpResponse;
        private readonly HeaderDictionary _headers;

        public ProjetDocumentControllerTests()
        {
            _docService = new Mock<IProjetDocumentService>();
            _fileService = new Mock<IFileService>();

            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpResponse = new Mock<HttpResponse>();
            _headers = new HeaderDictionary();
            _mockHttpResponse.Setup(r => r.Headers).Returns(_headers);
            _mockHttpContext.Setup(c => c.Response).Returns(_mockHttpResponse.Object);

            _controller = new ProjetDocumentController(_docService.Object, _fileService.Object)
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
            var list = new List<ReadProjetDocumentDto>
            {
                new() { id_projet_document = 1, id_projet = 1, url_projet_document = "/a.pdf", name_projet_document = "a", type_projet_document = "application/pdf", size_projet_document = 1, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow },
                new() { id_projet_document = 2, id_projet = 1, url_projet_document = "/b.pdf", name_projet_document = "b", type_projet_document = "application/pdf", size_projet_document = 1, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow }
            };
            _docService.Setup(s => s.GetProjetDocumentsByProjetId(1, 100, 0)).ReturnsAsync(list);
            _docService.Setup(s => s.GetProjetDocumentsCountByProjetId(1)).ReturnsAsync(list.Count);

            var res = await _controller.GetProjetsDocumentsByProjetId(1, 100, 0);

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsAssignableFrom<IEnumerable<ReadProjetDocumentDto>>(ok.Value);
            Assert.Equal(2, value.Count());
            Assert.Equal("2", _headers["X-Total-Count"].ToString());
        }

        [Fact]
        public async Task GetById_ReturnsOk_WithDoc()
        {
            var dto = new ReadProjetDocumentDto { id_projet_document = 5, id_projet = 2, url_projet_document = "/c.pdf", name_projet_document = "c", type_projet_document = "application/pdf", size_projet_document = 10, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _docService.Setup(s => s.GetProjetDocumentById(5, 2)).ReturnsAsync(dto);

            var res = await _controller.GetProjetDocumentById(5, 2);

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadProjetDocumentDto>(ok.Value);
            Assert.Equal(5, value.id_projet_document);
        }

        [Fact]
        public async Task Download_ReturnsFile_WhenSuccess()
        {
            var dto = new ReadProjetDocumentDto { id_projet_document = 7, id_projet = 3, url_projet_document = "/d.pdf", name_projet_document = "d.pdf", type_projet_document = "application/pdf", size_projet_document = 12, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _docService.Setup(s => s.GetProjetDocumentById(7, 3)).ReturnsAsync(dto);

            var ms = new MemoryStream(new byte[] { 1, 2, 3 });
            _fileService.Setup(f => f.GetFile("/d.pdf")).ReturnsAsync(new GetFileResult { FileStream = ms, MimeType = "application/pdf", Success = true, ErrorMessage = null });

            var res = await _controller.DownloadProjetDocument(7, 3);
            Assert.IsType<FileStreamResult>(res);
        }

        [Fact]
        public async Task Update_ReturnsOk()
        {
            var updated = new ReadProjetDocumentDto { id_projet_document = 9, id_projet = 4, url_projet_document = "/x.pdf", name_projet_document = "x", type_projet_document = "application/pdf", size_projet_document = 1, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _docService.Setup(s => s.UpdateProjetDocument(9, It.IsAny<UpdateProjetDocumentDto>(), 4)).ReturnsAsync(updated);

            var res = await _controller.UpdateProjetDocument(9, new UpdateProjetDocumentDto { name_projet_document = "x" }, 4);
            var ok = Assert.IsType<OkObjectResult>(res.Result);
            Assert.IsType<ReadProjetDocumentDto>(ok.Value);
        }

        [Fact]
        public async Task Delete_ReturnsNoContent()
        {
            _docService.Setup(s => s.DeleteProjetDocument(11, 5)).Returns(Task.CompletedTask);
            var res = await _controller.DeleteProjetDocument(11, 5);
            Assert.IsType<NoContentResult>(res);
        }
    }
}
