using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ItemTagService;
using Swashbuckle.AspNetCore.Annotations;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/item/{id_item}/tag")]

    public class ItemTagController : ControllerBase
    {
        private readonly IItemTagService _itemTagService;

        public ItemTagController(IItemTagService itemTagService)
        {
            _itemTagService = itemTagService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadExtendedItemTagDto>>> GetItemsTagsByItemId([FromRoute] int id_item, [FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'tag', 'item'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var itemTags = await _itemTagService.GetItemsTagsByItemId(id_item, limit, offset, expand);
            var CountList = await _itemTagService.GetItemsTagsCountByItemId(id_item);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");
            return Ok(itemTags);
        }

        [HttpGet("{id_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedItemTagDto>> GetItemTagById([FromRoute] int id_item, [FromRoute] int id_tag, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'tag', 'item'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var itemTag = await _itemTagService.GetItemTagById(id_item, id_tag, expand);
            return Ok(itemTag);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadItemTagDto>> CreateItemTag([FromRoute] int id_item, [FromBody] CreateItemTagByItemDto itemTagDto)
        {
            var itemTagDtoFull = new CreateItemTagDto
            {
                id_item = id_item,
                id_tag = itemTagDto.id_tag
            };
            var itemTag = await _itemTagService.CreateItemTag(itemTagDtoFull);
            return CreatedAtAction(nameof(GetItemTagById), new { id_item = itemTag.id_item, id_tag = itemTag.id_tag }, itemTag);
        }

        [HttpPost("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkItemTagDto>> CreateBulkItemTag([FromRoute] int id_item, [FromBody] List<CreateItemTagByItemDto> itemTagsDto)
        {
            var itemTagsDtoFull = itemTagsDto.Select(itemTagDto => new CreateItemTagDto
            {
                id_item = id_item,
                id_tag = itemTagDto.id_tag
            }).ToList();
            var itemTags = await _itemTagService.CreateBulkItemTag(itemTagsDtoFull);
            return Ok(itemTags);
        }

        [HttpDelete("{id_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteItemTag([FromRoute] int id_item, [FromRoute] int id_tag)
        {
            await _itemTagService.DeleteItemTag(id_item, id_tag);
            return NoContent();
        }
        
        [HttpDelete("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkItemTagDto>> DeleteBulkItemTag([FromRoute] int id_item, [FromBody] List<int> id_tags)
        {
            var itemTagsDtoFull = id_tags.Select(id_tag => new CreateItemTagDto
            {
                id_item = id_item,
                id_tag = id_tag
            }).ToList();
            var itemTags = await _itemTagService.DeleteBulkItemTag(itemTagsDtoFull);
            return Ok(itemTags);
        }
    }
}