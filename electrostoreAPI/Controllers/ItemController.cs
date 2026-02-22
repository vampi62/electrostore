using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ItemService;
using Swashbuckle.AspNetCore.Annotations;
using electrostore.Extensions;

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
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedItemDto>>> GetItems([FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'item_tags', 'item_boxs', 'command_items', 'projet_items', 'item_documents'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to select list of ID to research in the base. Multiple values can be specified by separating them with ','.")] List<int>? idResearch = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'nom_item=like=example'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'nom_item,asc' or 'nom_item,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var items = await _itemService.GetItems(limit, offset, rsqlDto, sortDto, expand, idResearch);
            return Ok(items);
        }

        [HttpGet("{id_item}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedItemDto>> GetItemById([FromRoute] int id_item,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'item_tags', 'item_boxs', 'command_items', 'projet_items', 'item_documents'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var item = await _itemService.GetItemById(id_item, expand);
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