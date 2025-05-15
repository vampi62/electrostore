using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ImgService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/item/{id_item}/img")]

    public class ItemImgController : ControllerBase
    {
        private readonly IImgService _imgService;

        public ItemImgController(IImgService imgService)
        {
            _imgService = imgService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadImgDto>>> GetImgsByItemId([FromRoute] int id_item, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var itemImgs = await _imgService.GetImgsByItemId(id_item, limit, offset);
            var CountList = await _imgService.GetImgsCountByItemId(id_item);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");
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
            var result = await _imgService.GetImageFile(img.url_picture_img); // check if img.url_picture_img is a valid path
            if (result.Success)
            {
                return PhysicalFile(result.FilePath, result.MimeType);
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
            var result = await _imgService.GetImageFile(img.url_thumbnail_img); // check if img.url_thumbnail_img is a valid path
            if (result.Success)
            {
                return PhysicalFile(result.FilePath, result.MimeType);
            }
            else
            {
                return NotFound(result.ErrorMessage);
            }
        }
    }
}