using Microsoft.AspNetCore.Mvc;
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
        public async Task<ActionResult<IEnumerable<ReadItemTagDto>>> GetItemsTagsByItemId([FromRoute] int id_item, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var itemTags = await _itemTagService.GetItemsTagsByItemId(id_item, limit, offset);
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

        [HttpGet("{id_tag}")]
        public async Task<ActionResult<ReadItemTagDto>> GetItemTagById([FromRoute] int id_item, [FromRoute] int id_tag)
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
        
        [HttpPost]
        public async Task<ActionResult<ReadItemTagDto>> CreateItemTags([FromRoute] int id_item, [FromBody] int[] tags)
        {
            var resultList = new List<ActionResult<ReadItemTagDto>>();
            for(int i = 0; i < tags.Length; i++)
            {
                var itemTagDto = new CreateItemTagDto
                {
                    id_tag = tags[i],
                    id_item = id_item
                };
                var itemTag = await _itemTagService.CreateItemTag(itemTagDto);
                if (itemTag.Result is BadRequestObjectResult || itemTag.Value == null)
                {
                    resultList.Add(itemTag.Result);
                }
                else
                {
                    resultList.Add(itemTag.Value);
                }
            }
            return Ok(resultList);
        }

        [HttpPost("{id_tag}")]
        public async Task<ActionResult<ReadItemTagDto>> CreateItemTag([FromRoute] int id_item, [FromRoute] int id_tag)
        {
            var itemTagDto = new CreateItemTagDto
            {
                id_item = id_item,
                id_tag = id_tag
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
            return CreatedAtAction(nameof(GetItemTagById), new { id_item = itemTag.Value.id_item, id_tag = itemTag.Value.id_tag }, itemTag.Value);
        }

        [HttpDelete("{id_tag}")]
        public async Task<ActionResult> DeleteItemTag([FromRoute] int id_item, [FromRoute] int id_tag)
        {
            await _itemTagService.DeleteItemTag(id_item, id_tag);
            return NoContent();
        }
    }
}