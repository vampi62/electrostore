using Microsoft.AspNetCore.Mvc;
using electrostore.Dto;
using electrostore.Services.TagService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/tag")]

    public class TagController : ControllerBase
    {
        private readonly ITagService _tagService;

        public TagController(ITagService tagService)
        {
            _tagService = tagService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReadTagDto>>> GetTags([FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var tags = await _tagService.GetTags(limit, offset);
            return Ok(tags);
        }

        [HttpGet("{id_tag}")]
        public async Task<ActionResult<ReadTagDto>> GetTagById([FromRoute] int id_tag)
        {
            var tag = await _tagService.GetTagById(id_tag);
            if (tag.Result is BadRequestObjectResult)
            {
                return tag.Result;
            }
            if (tag.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(tag.Value);
        }

        [HttpPost]
        public async Task<ActionResult<ReadTagDto>> CreateTag([FromBody] CreateTagDto tag)
        {
            var newTag = await _tagService.CreateTag(tag);
            if (newTag.Result is BadRequestObjectResult)
            {
                return newTag.Result;
            }
            if (newTag.Value == null)
            {
                return StatusCode(500);
            }
            return CreatedAtAction(nameof(GetTagById), new { id_tag = newTag.Value.id_tag }, newTag.Value);
        }

        [HttpPut("{id_tag}")]
        public async Task<ActionResult<ReadTagDto>> UpdateTag([FromRoute] int id_tag, [FromBody] UpdateTagDto tag)
        {
            var tagToUpdate = await _tagService.UpdateTag(id_tag, tag);
            if (tagToUpdate.Result is BadRequestObjectResult)
            {
                return tagToUpdate.Result;
            }
            if (tagToUpdate.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(tagToUpdate.Value);
        }
        
        [HttpDelete("{id_tag}")]
        public async Task<ActionResult> DeleteTag([FromRoute] int id_tag)
        {
            await _tagService.DeleteTag(id_tag);
            return NoContent();
        }
    }
}