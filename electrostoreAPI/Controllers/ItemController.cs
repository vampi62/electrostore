using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ItemService;
using Swashbuckle.AspNetCore.Annotations;

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
        public async Task<ActionResult<IEnumerable<ReadExtendedItemDto>>> GetItems([FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'item_tags', 'item_boxs', 'command_items', 'projet_items', 'item_documents'. Multiple values can be specified by separating them with ','.")] string? expand = null, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to select list of ID to research in the base. Multiple values can be specified by separating them with ','.")] string? idResearch = null)
        {
            var idList = string.IsNullOrWhiteSpace(idResearch) ? null : idResearch.Split(',').Where(id => int.TryParse(id, out _)).Select(int.Parse).ToList();
            var items = await _itemService.GetItems(limit, offset, expand?.Split(',').ToList(), idList);
            var CountList = await _itemService.GetItemsCount();
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers","X-Total-Count");
            return Ok(items);
        }

        [HttpGet("{id_item}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedItemDto>> GetItemById([FromRoute] int id_item, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'item_tags', 'item_boxs', 'command_items', 'projet_items', 'item_documents'. Multiple values can be specified by separating them with ','.")] string? expand = null)
        {
            var item = await _itemService.GetItemById(id_item, expand?.Split(',').ToList());
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