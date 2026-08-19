using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.ItemVendorPriceService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    public class ItemVendorPriceController : ControllerBase
    {
        private readonly IItemVendorPriceService _itemVendorPriceService;

        public ItemVendorPriceController(IItemVendorPriceService itemVendorPriceService)
        {
            _itemVendorPriceService = itemVendorPriceService;
        }

        [HttpGet("api/item/{id_item}/vendor/{id_item_vendor}/price-history")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedItemVendorPriceDto>>> GetPriceHistoryByItemVendorId(
            [FromRoute] int id_item,
            [FromRoute] int id_item_vendor,
            [FromQuery] int limit = 100,
            [FromQuery] int offset = 0,
            [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results.")] string? filter = null,
            [FromQuery, SwaggerParameter(Description = "(Optional) Sort string. Example: 'created_at,desc'.")] string? sort = null,
            [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'item_vendor'.")] List<string>? expand = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var priceHistory = await _itemVendorPriceService.GetPriceHistoryByItemVendorId(id_item_vendor, limit, offset, rsqlDto, sortDto, expand);
            return Ok(priceHistory);
        }

        [HttpGet("api/item/{id_item}/vendor/{id_item_vendor}/price-history/{id_item_vendor_price}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedItemVendorPriceDto>> GetPriceHistoryById(
            [FromRoute] int id_item,
            [FromRoute] int id_item_vendor,
            [FromRoute] int id_item_vendor_price,
            [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'item_vendor'.")] List<string>? expand = null)
        {
            var price = await _itemVendorPriceService.GetPriceHistoryById(id_item_vendor_price, id_item_vendor, expand);
            return Ok(price);
        }

        [HttpGet("api/item/vendor/price-history")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedItemVendorPriceDto>>> GetPriceHistory(
            [FromQuery] int limit = 100,
            [FromQuery] int offset = 0,
            [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results.")] string? filter = null,
            [FromQuery, SwaggerParameter(Description = "(Optional) Sort string. Example: 'created_at,desc'.")] string? sort = null,
            [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'item_vendor'.")] List<string>? expand = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var priceHistory = await _itemVendorPriceService.GetPriceHistory(limit, offset, rsqlDto, sortDto, expand);
            return Ok(priceHistory);
        }
    }
}
