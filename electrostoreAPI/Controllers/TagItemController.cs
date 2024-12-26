using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ItemTagService;
using Swashbuckle.AspNetCore.Annotations;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/tag/{id_tag}/item")]

    public class TagItemController : ControllerBase
    {
        private readonly IItemTagService _itemTagService;

        public TagItemController(IItemTagService itemTagService)
        {
            _itemTagService = itemTagService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadExtendedItemTagDto>>> GetItemsTagsByTagId([FromRoute] int id_tag, [FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery, SwaggerParameter(Description = "Fields to expand. Possible values: 'tag', 'item'. Multiple values can be specified by separating them with ','. Default: \"\"")] string expand = "")
        {
            var itemTags = await _itemTagService.GetItemsTagsByTagId(id_tag, limit, offset, expand.Split(',').ToList());
            var CountList = await _itemTagService.GetItemsTagsCountByTagId(id_tag);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers","X-Total-Count");
            return Ok(itemTags);
        }

        [HttpGet("{id_item}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedItemTagDto>> GetItemTagById([FromRoute] int id_tag, [FromRoute] int id_item, [FromQuery, SwaggerParameter(Description = "Fields to expand. Possible values: 'tag', 'item'. Multiple values can be specified by separating them with ','. Default: \"\"")] string expand = "")
        {
            var itemTag = await _itemTagService.GetItemTagById(id_item, id_tag, expand.Split(',').ToList());
            return Ok(itemTag);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadItemTagDto>> CreateItemTag([FromRoute] int id_tag, [FromBody] CreateItemTagByTagDto itemTagDto)
        {
            var itemTagDtoFull = new CreateItemTagDto
            {
                id_tag = id_tag,
                id_item = itemTagDto.id_item
            };
            var itemTag = await _itemTagService.CreateItemTag(itemTagDtoFull);
            return CreatedAtAction(nameof(GetItemTagById), new { id_tag = itemTag.id_tag, id_item = itemTag.id_item }, itemTag);
        }

        [HttpPost("bulk")]
		[Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkItemTagDto>> CreateBulkItemTag([FromRoute] int id_tag, [FromBody] List<CreateItemTagByTagDto> itemTagsDto)
        {
            var itemTagsDtoFull = itemTagsDto.Select(itemTagDto => new CreateItemTagDto
            {
                id_tag = id_tag,
                id_item = itemTagDto.id_item
            }).ToList();
            var itemTags = await _itemTagService.CreateBulkItemTag(itemTagsDtoFull);
            return Ok(itemTags);
        }

        [HttpDelete("{id_item}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteItemTag([FromRoute] int id_tag, [FromRoute] int id_item)
        {
            await _itemTagService.DeleteItemTag(id_item, id_tag);
            return NoContent();
        }

        [HttpDelete("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkItemTagDto>> DeleteBulkItemTag([FromRoute] int id_tag, [FromBody] List<int> id_items)
        {
            var itemTagsDtoFull = id_items.Select(id_item => new CreateItemTagDto
            {
                id_item = id_item,
                id_tag = id_tag
            }).ToList();
            var itemTags = await _itemTagService.DeleteBulkItemTag(itemTagsDtoFull);
            return Ok(itemTags);
        }
    }
}