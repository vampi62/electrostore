using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Services.ItemService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/item")]

    public class ItemController : ControllerBase
    {
        private readonly IItemService _itemService;
        private readonly IFileService _fileService;

        public ItemController(IItemService itemService, IFileService fileService)
        {
            _itemService = itemService;
            _fileService = fileService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedItemDto>>> GetItems([FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'item_tags', 'item_boxs', 'command_items', 'project_items', 'item_documents', 'item_history'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
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
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'item_tags', 'item_boxs', 'command_items', 'project_items', 'item_documents', 'item_history'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var item = await _itemService.GetItemById(id_item, expand);
            return Ok(item);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadItemDto>> CreateItem([FromForm] CreateItemDto itemDto)
        {
            var item = await _itemService.CreateItem(itemDto);
            return CreatedAtAction(nameof(GetItemById), new { id_item = item.id_item }, item);
        }

        [HttpPut("{id_item}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadItemDto>> UpdateItem([FromRoute] int id_item, [FromForm] UpdateItemDto itemDto)
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

        [HttpGet("{id_item}/picture")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> GetItemPicture([FromRoute] int id_item)
        {
            var item = await _itemService.GetItemById(id_item);
            if (string.IsNullOrEmpty(item.url_picture_item))
            {
                return NotFound();
            }
            var result = await _fileService.GetFile(item.url_picture_item);
            if (result.success && result.file_stream != null)
            {
                return File(result.file_stream, result.mime_type);
            }
            else
            {
                return NotFound(result.error_message);
            }
        }

        [HttpGet("{id_item}/thumbnail")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> GetItemThumbnail([FromRoute] int id_item)
        {
            var item = await _itemService.GetItemById(id_item);
            if (string.IsNullOrEmpty(item.url_thumbnail_item))
            {
                return NotFound();
            }
            var result = await _fileService.GetFile(item.url_thumbnail_item);
            if (result.success && result.file_stream != null)
            {
                return File(result.file_stream, result.mime_type);
            }
            else
            {
                return NotFound(result.error_message);
            }
        }
    }
}