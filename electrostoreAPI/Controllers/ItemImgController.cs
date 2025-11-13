using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ImgService;
using electrostore.Services.FileService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/item/{id_item}/img")]

    public class ItemImgController : ControllerBase
    {
        private readonly IImgService _imgService;
        private readonly IFileService _fileService;

        public ItemImgController(IImgService imgService, IFileService fileService)
        {
            _imgService = imgService;
            _fileService = fileService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadImgDto>>> GetImgsByItemId([FromRoute] int id_item, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var itemImgs = await _imgService.GetImgsByItemId(id_item, limit, offset);
            var CountList = await _imgService.GetImgsCountByItemId(id_item);
            Response.Headers["X-Total-Count"] = CountList.ToString();
            Response.Headers.AccessControlExposeHeaders = "X-Total-Count";
            return Ok(itemImgs);
        }

        [HttpGet("{id_img}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadImgDto>> GetImgById([FromRoute] int id_item, [FromRoute] int id_img)
        {
            var itemImg = await _imgService.GetImgById(id_img, id_item);
            return Ok(itemImg);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadImgDto>> CreateImg([FromRoute] int id_item, [FromForm] CreateImgByItemDto itemImgDto)
        {
            var itemImgDtoFull = new CreateImgDto
            {
                id_item = id_item,
                img_file = itemImgDto.img_file,
                nom_img = itemImgDto.nom_img,
                description_img = itemImgDto.description_img
            };
            var itemImg = await _imgService.CreateImg(itemImgDtoFull);
            return CreatedAtAction(nameof(GetImgById), new { id_item = itemImg.id_item, id_img = itemImg.id_img }, itemImg);
        }

        [HttpPut("{id_img}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadImgDto>> UpdateImg([FromRoute] int id_item, [FromRoute] int id_img, [FromBody] UpdateImgDto itemImgDto)
        {
            var itemImg = await _imgService.UpdateImg(id_img, itemImgDto, id_item);
            return Ok(itemImg);
        }

        [HttpDelete("{id_img}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteImg([FromRoute] int id_item, [FromRoute] int id_img)
        {
            await _imgService.DeleteImg(id_img, id_item);
            return NoContent();
        }

        [HttpGet("{id_img}/picture")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> GetImgData([FromRoute] int id_img, [FromRoute] int id_item)
        {
            var img = await _imgService.GetImgById(id_img, id_item);
            var result = await _fileService.GetFile(img.url_picture_img);
            if (result.Success && result.FileStream != null)
            {
                return File(result.FileStream, result.MimeType);
            }
            else
            {
                return NotFound(result.ErrorMessage);
            }
        }

        [HttpGet("{id_img}/thumbnail")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> GetImgThumbnail([FromRoute] int id_img, [FromRoute] int id_item)
        {
            var img = await _imgService.GetImgById(id_img, id_item);
            var result = await _fileService.GetFile(img.url_thumbnail_img);
            if (result.Success && result.FileStream != null)
            {
                return File(result.FileStream, result.MimeType);
            }
            else
            {
                return NotFound(result.ErrorMessage);
            }
        }
    }
}