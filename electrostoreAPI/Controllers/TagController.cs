using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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
        [Authorize(Policy = "AccessTokenPolicy")]
        public async Task<ActionResult<IEnumerable<ReadTagDto>>> GetTags([FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var tags = await _tagService.GetTags(limit, offset);
            return Ok(tags);
        }

        [HttpGet("{id_tag}")]
        [Authorize(Policy = "AccessTokenPolicy")]
        public async Task<ActionResult<ReadTagDto>> GetTagById([FromRoute] int id_tag)
        {
            var tag = await _tagService.GetTagById(id_tag);
            return Ok(tag);
        }

        [HttpPost]
        [Authorize(Policy = "AccessTokenPolicy")]
        public async Task<ActionResult<ReadTagDto>> CreateTag([FromBody] CreateTagDto tag)
        {
            var newTag = await _tagService.CreateTag(tag);
            return CreatedAtAction(nameof(GetTagById), new { id_tag = newTag.id_tag }, newTag);
        }

        [HttpPut("{id_tag}")]
        [Authorize(Policy = "AccessTokenPolicy")]
        public async Task<ActionResult<ReadTagDto>> UpdateTag([FromRoute] int id_tag, [FromBody] UpdateTagDto tag)
        {
            var tagToUpdate = await _tagService.UpdateTag(id_tag, tag);
            return Ok(tagToUpdate);
        }
        
        [HttpDelete("{id_tag}")]
        [Authorize(Policy = "AccessTokenPolicy")]
        public async Task<ActionResult> DeleteTag([FromRoute] int id_tag)
        {
            await _tagService.DeleteTag(id_tag);
            return NoContent();
        }
    }
}