using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ItemTagService;

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
        public async Task<ActionResult<IEnumerable<ReadItemTagDto>>> GetItemsTagsByItemId([FromRoute] int id_item, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var itemTags = await _itemTagService.GetItemsTagsByItemId(id_item, limit, offset);
            return Ok(itemTags);
        }

        [HttpGet("{id_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadItemTagDto>> GetItemTagById([FromRoute] int id_item, [FromRoute] int id_tag)
        {
            var itemTag = await _itemTagService.GetItemTagById(id_item, id_tag);
            return Ok(itemTag);
        }
        
        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadItemTagDto>> CreateItemTags([FromRoute] int id_item, [FromBody] int[] tags)
        {
            var itemTags = await _itemTagService.CreateItemTags(id_item, null, tags, null);
            return Ok(itemTags);
        }

        [HttpPost("{id_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadItemTagDto>> CreateItemTag([FromRoute] int id_item, [FromRoute] int id_tag)
        {
            var itemTagDto = new CreateItemTagDto
            {
                id_item = id_item,
                id_tag = id_tag
            };
            var itemTag = await _itemTagService.CreateItemTag(itemTagDto);
            return CreatedAtAction(nameof(GetItemTagById), new { id_item = itemTag.id_item, id_tag = itemTag.id_tag }, itemTag);
        }

        [HttpDelete("{id_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteItemTag([FromRoute] int id_item, [FromRoute] int id_tag)
        {
            await _itemTagService.DeleteItemTag(id_item, id_tag);
            return NoContent();
        }
    }
}