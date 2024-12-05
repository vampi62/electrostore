using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ItemBoxService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/item/{id_item}/box")]

    public class ItemBoxController : ControllerBase
    {
        private readonly IItemBoxService _itemBoxService;

        public ItemBoxController(IItemBoxService itemBoxService)
        {
            _itemBoxService = itemBoxService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadItemBoxDto>>> GetItemsBoxsByItemId([FromRoute] int id_item, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var itemBoxs = await _itemBoxService.GetItemsBoxsByItemId(id_item, limit, offset);
            var CountList = await _itemBoxService.GetItemsBoxsCountByItemId(id_item);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");
            return Ok(itemBoxs);
        }

        [HttpGet("{id_box}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadItemBoxDto>> GetItemBoxById([FromRoute] int id_item, [FromRoute] int id_box)
        {
            var itemBox = await _itemBoxService.GetItemBoxById(id_item, id_box);
            return Ok(itemBox);
        }

        [HttpPost("{id_box}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadItemBoxDto>> CreateItemBox([FromRoute] int id_item, [FromRoute] int id_box, [FromBody] CreateItemBoxByItemDto itemBoxDto)
        {
            var itemBoxDtoFull = new CreateItemBoxDto
            {
                id_item = id_item,
                id_box = id_box,
                qte_item_box = itemBoxDto.qte_item_box,
                seuil_max_item_item_box = itemBoxDto.seuil_max_item_item_box
            };
            var itemBox = await _itemBoxService.CreateItemBox(itemBoxDtoFull);
            return CreatedAtAction(nameof(GetItemBoxById), new { id_item = itemBox.id_item, id_box = itemBox.id_box }, itemBox);
        }

        [HttpPut("{id_box}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadItemBoxDto>> UpdateItemBox([FromRoute] int id_item, [FromRoute] int id_box, [FromBody] UpdateItemBoxDto itemBoxDto)
        {
            var itemBox = await _itemBoxService.UpdateItemBox(id_item, id_box, itemBoxDto);
            if (itemBoxDto.new_id_box != null)
            {
                return CreatedAtAction(nameof(GetItemBoxById), new { id_item = itemBox.id_item, id_box = itemBox.id_box }, itemBox);
            }
            return Ok(itemBox);
        }

        [HttpDelete("{id_box}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteItemBox([FromRoute] int id_item, [FromRoute] int id_box)
        {
            await _itemBoxService.DeleteItemBox(id_item, id_box);
            return NoContent();
        }
    }
}