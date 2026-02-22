using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ItemBoxService;
using Swashbuckle.AspNetCore.Annotations;
using electrostore.Extensions;

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
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedItemBoxDto>>> GetItemsBoxsByItemId([FromRoute] int id_item, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'item', 'box'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'qte_item_box=gt=1'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'qte_item_box,asc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var itemBoxs = await _itemBoxService.GetItemsBoxsByItemId(id_item, limit, offset, rsqlDto, sortDto, expand);
            return Ok(itemBoxs);
        }

        [HttpGet("{id_box}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedItemBoxDto>> GetItemBoxById([FromRoute] int id_item, [FromRoute] int id_box,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'item', 'box'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var itemBox = await _itemBoxService.GetItemBoxById(id_item, id_box, expand);
            return Ok(itemBox);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadItemBoxDto>> CreateItemBox([FromRoute] int id_item, [FromBody] CreateItemBoxByItemDto itemBoxDto)
        {
            var itemBoxDtoFull = new CreateItemBoxDto
            {
                id_item = id_item,
                id_box = itemBoxDto.id_box,
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