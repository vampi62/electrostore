using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using electrostore.Controllers;
using electrostore.Dto;
using electrostore.Services.ImgService;
using electrostore.Services.FileService;

namespace electrostore.Tests.Controllers
{
    public class ItemImgControllerTests
    {
        private readonly Mock<IImgService> _imgService;
        private readonly Mock<IFileService> _fileService;
        private readonly ItemImgController _controller;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<HttpResponse> _mockHttpResponse;
        private readonly HeaderDictionary _headers;

        public ItemImgControllerTests()
        {
            _imgService = new Mock<IImgService>();
            _fileService = new Mock<IFileService>();

            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpResponse = new Mock<HttpResponse>();
            _headers = new HeaderDictionary();
            _mockHttpResponse.Setup(r => r.Headers).Returns(_headers);
            _mockHttpContext.Setup(c => c.Response).Returns(_mockHttpResponse.Object);

            _controller = new ItemImgController(_imgService.Object, _fileService.Object)
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
            var list = new List<ReadImgDto>
            {
                new() { id_img = 1, nom_img = "a", url_picture_img = "/p1.jpg", url_thumbnail_img = "/t1.jpg", description_img = "d", id_item = 1, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow },
                new() { id_img = 2, nom_img = "b", url_picture_img = "/p2.jpg", url_thumbnail_img = "/t2.jpg", description_img = "d", id_item = 1, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow }
            };
            _imgService.Setup(s => s.GetImgsByItemId(1, 100, 0)).ReturnsAsync(list);
            _imgService.Setup(s => s.GetImgsCountByItemId(1)).ReturnsAsync(list.Count);

            var res = await _controller.GetImgsByItemId(1, 100, 0);

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsAssignableFrom<IEnumerable<ReadImgDto>>(ok.Value);
            Assert.Equal(2, value.Count());
            Assert.Equal("2", _headers["X-Total-Count"].ToString());
        }

        [Fact]
        public async Task GetById_ReturnsOk_WithImg()
        {
            var dto = new ReadImgDto { id_img = 5, nom_img = "z", url_picture_img = "/pz.jpg", url_thumbnail_img = "/tz.jpg", description_img = "d", id_item = 2, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _imgService.Setup(s => s.GetImgById(5, 2)).ReturnsAsync(dto);

            var res = await _controller.GetImgById(2, 5);

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadImgDto>(ok.Value);
            Assert.Equal(5, value.id_img);
        }

        [Fact]
        public async Task GetPicture_ReturnsFile_WhenSuccess()
        {
            var dto = new ReadImgDto { id_img = 6, nom_img = "p", url_picture_img = "/px.jpg", url_thumbnail_img = "/tx.jpg", description_img = "d", id_item = 3, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _imgService.Setup(s => s.GetImgById(6, 3)).ReturnsAsync(dto);
            var ms = new MemoryStream(new byte[] { 0x01 });
            _fileService.Setup(f => f.GetFile("/px.jpg")).ReturnsAsync(new GetFileResult { FileStream = ms, MimeType = "image/jpeg", Success = true, ErrorMessage = null });

            var res = await _controller.GetImgData(6, 3);
            Assert.IsType<FileStreamResult>(res);
        }

        [Fact]
        public async Task GetThumbnail_ReturnsFile_WhenSuccess()
        {
            var dto = new ReadImgDto { id_img = 7, nom_img = "t", url_picture_img = "/py.jpg", url_thumbnail_img = "/ty.jpg", description_img = "d", id_item = 3, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _imgService.Setup(s => s.GetImgById(7, 3)).ReturnsAsync(dto);
            var ms = new MemoryStream(new byte[] { 0x02 });
            _fileService.Setup(f => f.GetFile("/ty.jpg")).ReturnsAsync(new GetFileResult { FileStream = ms, MimeType = "image/jpeg", Success = true, ErrorMessage = null });

            var res = await _controller.GetImgThumbnail(7, 3);
            Assert.IsType<FileStreamResult>(res);
        }
    }
}
