using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ItemTagService;

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
        public async Task<ActionResult<IEnumerable<ReadItemTagDto>>> GetItemsTagsByTagId([FromRoute] int id_tag, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var itemTags = await _itemTagService.GetItemsTagsByTagId(id_tag, limit, offset);
            var CountList = await _itemTagService.GetItemsTagsCountByTagId(id_tag);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers","X-Total-Count");
            return Ok(itemTags);
        }

        [HttpGet("{id_item}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadItemTagDto>> GetItemTagById([FromRoute] int id_tag, [FromRoute] int id_item)
        {
            var itemTag = await _itemTagService.GetItemTagById(id_item, id_tag);
            return Ok(itemTag);
        }
        
        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadItemTagDto>> CreateItemsTag([FromRoute] int id_tag, [FromBody] int[] items)
        {
            var itemTags = await _itemTagService.CreateItemTags(null, id_tag, null, items);
            return Ok(itemTags);
        }
        

        [HttpPost("{id_item}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadItemTagDto>> CreateItemTag([FromRoute] int id_tag, [FromRoute] int id_item)
        {
            var itemTagDto = new CreateItemTagDto
            {
                id_tag = id_tag,
                id_item = id_item
            };
            var itemTag = await _itemTagService.CreateItemTag(itemTagDto);
            return CreatedAtAction(nameof(GetItemTagById), new { id_tag = itemTag.id_tag, id_item = itemTag.id_item }, itemTag);
        }

        [HttpDelete("{id_item}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteItemTag([FromRoute] int id_tag, [FromRoute] int id_item)
        {
            await _itemTagService.DeleteItemTag(id_item, id_tag);
            return NoContent();
        }
    }
}