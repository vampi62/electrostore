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
        public async Task<ActionResult<IEnumerable<ReadItemTagDto>>> GetItemsTagsByTagId([FromRoute] int id_tag, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var itemTags = await _itemTagService.GetItemsTagsByTagId(id_tag, limit, offset);
            if (itemTags.Result is BadRequestObjectResult)
            {
                return itemTags.Result;
            }
            if (itemTags.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(itemTags.Value);
        }

        [HttpGet("{id_item}")]
        public async Task<ActionResult<ReadItemTagDto>> GetItemTagById([FromRoute] int id_tag, [FromRoute] int id_item)
        {
            var itemTag = await _itemTagService.GetItemTagById(id_item, id_tag);
            if (itemTag.Result is BadRequestObjectResult)
            {
                return itemTag.Result;
            }
            if (itemTag.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(itemTag.Value);
        }

        [HttpPost("{id_item}")]
        public async Task<ActionResult<ReadItemTagDto>> CreateItemTag([FromRoute] int id_tag, [FromRoute] int id_item)
        {
            var itemTagDto = new CreateItemTagDto
            {
                id_tag = id_tag,
                id_item = id_item
            };
            var itemTag = await _itemTagService.CreateItemTag(itemTagDto);
            if (itemTag.Result is BadRequestObjectResult)
            {
                return itemTag.Result;
            }
            if (itemTag.Value == null)
            {
                return StatusCode(500);
            }
            return CreatedAtAction(nameof(GetItemTagById), new { id_tag = itemTag.Value.id_tag, id_item = itemTag.Value.id_item }, itemTag.Value);
        }

        [HttpDelete("{id_item}")]
        public async Task<ActionResult> DeleteItemTag([FromRoute] int id_tag, [FromRoute] int id_item)
        {
            await _itemTagService.DeleteItemTag(id_item, id_tag);
            return NoContent();
        }
    }
}