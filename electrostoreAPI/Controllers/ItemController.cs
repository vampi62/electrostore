using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadItemDto>>> GetItems([FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var items = await _itemService.GetItems(limit, offset);
            return Ok(items);
        }

        [HttpGet("{id_item}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadItemDto>> GetItemById([FromRoute] int id_item)
        {
            var item = await _itemService.GetItemById(id_item);
            return Ok(item);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadItemDto>> CreateItem([FromBody] CreateItemDto itemDto)
        {
            var item = await _itemService.CreateItem(itemDto);
            return CreatedAtAction(nameof(GetItemById), new { id_item = item.id_item }, item);
        }

        [HttpPut("{id_item}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadItemDto>> UpdateItem([FromRoute] int id_item, [FromBody] UpdateItemDto itemDto)
        {
            var item = await _itemService.UpdateItem(id_item, itemDto);
            return Ok(item);
        }

        [HttpDelete("{id_item}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteItem([FromRoute] int id_item)
        {
            await _itemService.DeleteItem(id_item);
            return NoContent();
        }
    }
}