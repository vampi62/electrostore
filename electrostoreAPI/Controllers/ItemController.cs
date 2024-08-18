using Microsoft.AspNetCore.Mvc;
using electrostore.Dto;
using electrostore.Services.ItemService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/item")]

    public class ItemController : ControllerBase
    {
        private readonly IItemService _itemService;

        public ItemController(IItemService itemService)
        {
            _itemService = itemService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReadItemDto>>> GetItems([FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var items = await _itemService.GetItems(limit, offset);
            return Ok(items);
        }

        [HttpGet("{id_item}")]
        public async Task<ActionResult<ReadItemDto>> GetItemById([FromRoute] int id_item)
        {
            var item = await _itemService.GetItemById(id_item);
            if (item.Result is BadRequestObjectResult)
            {
                return item.Result;
            }
            if (item.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(item.Value);
        }

        [HttpPost]
        public async Task<ActionResult<ReadItemDto>> CreateItem([FromBody] CreateItemDto itemDto)
        {
            var item = await _itemService.CreateItem(itemDto);
            if (item.Result is BadRequestObjectResult)
            {
                return item.Result;
            }
            if (item.Value == null)
            {
                return StatusCode(500);
            }
            return CreatedAtAction(nameof(GetItemById), new { id_item = item.Value.id_item }, item.Value);
        }

        [HttpPut("{id_item}")]
        public async Task<ActionResult<ReadItemDto>> UpdateItem([FromRoute] int id_item, [FromBody] UpdateItemDto itemDto)
        {
            var item = await _itemService.UpdateItem(id_item, itemDto);
            if (item.Result is BadRequestObjectResult)
            {
                return item.Result;
            }
            if (item.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(item.Value);
        }

        [HttpDelete("{id_item}")]
        public async Task<ActionResult> DeleteItem([FromRoute] int id_item)
        {
            await _itemService.DeleteItem(id_item);
            return NoContent();
        }
    }
}