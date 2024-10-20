using Microsoft.AspNetCore.Mvc;
using electrostore.Dto;
using electrostore.Services.ItemBoxService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/box/{id_box}/item")]

    public class BoxItemController : ControllerBase
    {
        private readonly IItemBoxService _itemBoxService;

        public BoxItemController(IItemBoxService itemBoxService)
        {
            _itemBoxService = itemBoxService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReadItemBoxDto>>> GetItemsBoxsByBoxId([FromRoute] int id_box, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var itemsBoxs = await _itemBoxService.GetItemsBoxsByBoxId(id_box, limit, offset);
            return Ok(itemsBoxs);
        }

        [HttpGet("{id_item}")]
        public async Task<ActionResult<ReadItemBoxDto>> GetItemBoxById([FromRoute] int id_box, [FromRoute] int id_item)
        {
            var itemBox = await _itemBoxService.GetItemBoxById(id_box, id_item);
            return Ok(itemBox);
        }

        [HttpPost("{id_item}")]
        public async Task<ActionResult<ReadItemBoxDto>> CreateItemBox([FromRoute] int id_box, [FromRoute] int id_item, [FromBody] CreateItemBoxByBoxDto itemBoxDto)
        {
            var itemBoxDtoFull = new CreateItemBoxDto
            {
                id_box = id_box,
                id_item = id_item,
                qte_itembox = itemBoxDto.qte_itembox,
                seuil_max_itemitembox = itemBoxDto.seuil_max_itemitembox
            };
            var itemBox = await _itemBoxService.CreateItemBox(itemBoxDtoFull);
            return CreatedAtAction(nameof(GetItemBoxById), new { id_box = itemBox.id_box, id_item = itemBox.id_item }, itemBox);
        }

        [HttpPut("{id_item}")]
        public async Task<ActionResult<ReadItemBoxDto>> UpdateItemBox([FromRoute] int id_box, [FromRoute] int id_item, [FromBody] UpdateItemBoxDto itemBoxDto)
        {
            var itemBox = await _itemBoxService.UpdateItemBox(id_box, id_item, itemBoxDto);
            if (itemBoxDto.new_id_box != null)
            {
                return CreatedAtAction(nameof(GetItemBoxById), new { id_box = itemBox.id_box, id_item = itemBox.id_item }, itemBox);
            }
            return Ok(itemBox);
        }

        [HttpDelete("{id_item}")]
        public async Task<ActionResult> DeleteItemBox([FromRoute] int id_box, [FromRoute] int id_item)
        {
            await _itemBoxService.DeleteItemBox(id_box, id_item);
            return NoContent();
        }
    }
}