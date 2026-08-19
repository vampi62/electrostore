using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.ItemVendorService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    public class ItemVendorController : ControllerBase
    {
        private readonly IItemVendorService _itemVendorService;

        public ItemVendorController(IItemVendorService itemVendorService)
        {
            _itemVendorService = itemVendorService;
        }

        [HttpGet("api/item/{id_item}/vendor")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedItemVendorDto>>> GetItemVendorsByItemId(
            [FromRoute] int id_item,
            [FromQuery] int limit = 100,
            [FromQuery] int offset = 0,
            [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results.")] string? filter = null,
            [FromQuery, SwaggerParameter(Description = "(Optional) Sort string. Example: 'vendor_sku_item_vendor,asc'.")] string? sort = null,
            [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'item'.")] List<string>? expand = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var itemVendors = await _itemVendorService.GetItemVendorsByItemId(id_item, limit, offset, rsqlDto, sortDto, expand);
            return Ok(itemVendors);
        }

        [HttpGet("api/item/{id_item}/vendor/{id_item_vendor}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedItemVendorDto>> GetItemVendorById(
            [FromRoute] int id_item,
            [FromRoute] int id_item_vendor,
            [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'item'.")] List<string>? expand = null)
        {
            var itemVendor = await _itemVendorService.GetItemVendorById(id_item_vendor, id_item, expand);
            return Ok(itemVendor);
        }

        [HttpGet("api/item/vendor")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedItemVendorDto>>> GetItemVendors(
            [FromQuery] int limit = 100,
            [FromQuery] int offset = 0,
            [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results.")] string? filter = null,
            [FromQuery, SwaggerParameter(Description = "(Optional) Sort string. Example: 'vendor_sku_item_vendor,asc'.")] string? sort = null,
            [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'item'.")] List<string>? expand = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var itemVendors = await _itemVendorService.GetItemVendors(limit, offset, rsqlDto, sortDto, expand);
            return Ok(itemVendors);
        }

        [HttpPost("api/item/{id_item}/vendor")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadItemVendorDto>> CreateItemVendor(
            [FromRoute] int id_item,
            [FromBody] CreateItemVendorDto itemVendorDto)
        {
            var itemVendorDtoFull = itemVendorDto with { id_item = id_item };
            var itemVendor = await _itemVendorService.CreateItemVendor(itemVendorDtoFull);
            return CreatedAtAction(nameof(GetItemVendorById), new { id_item_vendor = itemVendor.id_item_vendor, id_item = itemVendor.id_item }, itemVendor);
        }

        [HttpPut("api/item/{id_item}/vendor/{id_item_vendor}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadItemVendorDto>> UpdateItemVendor(
            [FromRoute] int id_item,
            [FromRoute] int id_item_vendor,
            [FromBody] UpdateItemVendorDto itemVendorDto)
        {
            var itemVendor = await _itemVendorService.UpdateItemVendor(id_item_vendor, itemVendorDto, id_item);
            return Ok(itemVendor);
        }

        [HttpDelete("api/item/{id_item}/vendor/{id_item_vendor}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteItemVendor(
            [FromRoute] int id_item,
            [FromRoute] int id_item_vendor)
        {
            await _itemVendorService.DeleteItemVendor(id_item_vendor, id_item);
            return NoContent();
        }
    }
}
