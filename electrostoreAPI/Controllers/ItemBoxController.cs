using Microsoft.AspNetCore.Mvc;
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
        public async Task<ActionResult<IEnumerable<ReadItemBoxDto>>> GetItemsBoxsByItemId([FromRoute] int id_item, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var itemBoxs = await _itemBoxService.GetItemsBoxsByItemId(id_item, limit, offset);
            return Ok(itemBoxs);
        }

        [HttpGet("{id_box}")]
        public async Task<ActionResult<ReadItemBoxDto>> GetItemBoxById([FromRoute] int id_item, [FromRoute] int id_box)
        {
            var itemBox = await _itemBoxService.GetItemBoxById(id_item, id_box);
            return Ok(itemBox);
        }

        [HttpPost("{id_box}")]
        public async Task<ActionResult<ReadItemBoxDto>> CreateItemBox([FromRoute] int id_item, [FromRoute] int id_box, [FromBody] CreateItemBoxByItemDto itemBoxDto)
        {
            var itemBoxDtoFull = new CreateItemBoxDto
            {
                id_item = id_item,
                id_box = id_box,
                qte_itembox = itemBoxDto.qte_itembox,
                seuil_max_itemitembox = itemBoxDto.seuil_max_itemitembox
            };
            var itemBox = await _itemBoxService.CreateItemBox(itemBoxDtoFull);
            return CreatedAtAction(nameof(GetItemBoxById), new { id_item = itemBox.id_item, id_box = itemBox.id_box }, itemBox);
        }

        [HttpPut("{id_box}")]
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
        public async Task<ActionResult> DeleteItemBox([FromRoute] int id_item, [FromRoute] int id_box)
        {
            await _itemBoxService.DeleteItemBox(id_item, id_box);
            return NoContent();
        }
    }
}