using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.ItemHistoryService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    public class ItemHistoryController : ControllerBase
    {
        private readonly IItemHistoryService _itemHistoryService;

        public ItemHistoryController(IItemHistoryService itemHistoryService)
        {
            _itemHistoryService = itemHistoryService;
        }

        [HttpGet("api/item/{id_item}/history")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedItemHistoryDto>>> GetItemHistoryByItemId(
            [FromRoute] int id_item,
            [FromQuery] int limit = 100,
            [FromQuery] int offset = 0,
            [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results.")] string? filter = null,
            [FromQuery, SwaggerParameter(Description = "(Optional) Sort string. Example: 'created_at,desc'.")] string? sort = null,
            [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'item', 'box', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var history = await _itemHistoryService.GetItemHistoryByItemId(id_item, limit, offset, rsqlDto, sortDto, expand);
            return Ok(history);
        }

        [HttpGet("api/item/{id_item}/history/{id_history}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedItemHistoryDto>> GetItemHistoryById(
            [FromRoute] int id_item,
            [FromRoute] int id_history,
            [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'item', 'box', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var history = await _itemHistoryService.GetItemHistoryById(id_history, id_item, expand);
            return Ok(history);
        }

        [HttpGet("api/item/history")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedItemHistoryDto>>> GetItemsHistory(
            [FromQuery] int limit = 100,
            [FromQuery] int offset = 0,
            [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results.")] string? filter = null,
            [FromQuery, SwaggerParameter(Description = "(Optional) Sort string. Example: 'created_at,desc'.")] string? sort = null,
            [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'item', 'box', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var history = await _itemHistoryService.GetItemsHistory(limit, offset, rsqlDto, sortDto, expand);
            return Ok(history);
        }
    }
}
