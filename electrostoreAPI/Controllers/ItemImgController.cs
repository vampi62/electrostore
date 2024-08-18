using Microsoft.AspNetCore.Mvc;
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
        public async Task<ActionResult<IEnumerable<ReadImgDto>>> GetImgsByItemId([FromRoute] int id_item, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var itemImgs = await _imgService.GetImgsByItemId(id_item, limit, offset);
            if (itemImgs.Result is BadRequestObjectResult)
            {
                return itemImgs.Result;
            }
            if (itemImgs.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(itemImgs.Value);
        }

        [HttpGet("{id_img}")]
        public async Task<ActionResult<ReadImgDto>> GetImgById([FromRoute] int id_item, [FromRoute] int id_img)
        {
            var itemImg = await _imgService.GetImgById(id_img, id_item);
            if (itemImg.Result is BadRequestObjectResult)
            {
                return itemImg.Result;
            }
            if (itemImg.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(itemImg.Value);
        }

        [HttpPost]
        public async Task<ActionResult<ReadImgDto>> CreateImg([FromRoute] int id_item, [FromBody] CreateImgByItemDto itemImgDto)
        {
            var itemImgDtoFull = new CreateImgDto
            {
                id_item = id_item,
                nom_img = itemImgDto.nom_img,
                description_img = itemImgDto.description_img
            };
            var itemImg = await _imgService.CreateImg(itemImgDtoFull);
            if (itemImg.Result is BadRequestObjectResult)
            {
                return itemImg.Result;
            }
            if (itemImg.Value == null)
            {
                return StatusCode(500);
            }
            return CreatedAtAction(nameof(GetImgById), new { id_item = itemImg.Value.id_item, id_img = itemImg.Value.id_img }, itemImg.Value);
        }

        [HttpPut("{id_img}")]
        public async Task<ActionResult<ReadImgDto>> UpdateImg([FromRoute] int id_item, [FromRoute] int id_img, [FromBody] UpdateImgDto itemImgDto)
        {
            var itemImg = await _imgService.UpdateImg(id_img, itemImgDto, id_item);
            if (itemImg.Result is BadRequestObjectResult)
            {
                return itemImg.Result;
            }
            if (itemImg.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(itemImg.Value);
        }

        [HttpDelete("{id_img}")]
        public async Task<ActionResult> DeleteImg([FromRoute] int id_item, [FromRoute] int id_img)
        {
            await _imgService.DeleteImg(id_img, id_item);
            return NoContent();
        }
    }
}