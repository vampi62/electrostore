using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.TagService;
using Swashbuckle.AspNetCore.Annotations;

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
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadExtendedTagDto>>> GetTags([FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery, SwaggerParameter(Description = "Fields to expand. Possible values: 'stores_tags', 'items_tags', 'boxs_tags'. Multiple values can be specified by separating them with ','. Default: \"\"")] string expand = "", [FromQuery] string idResearch = "")
        {
            var idList = string.IsNullOrWhiteSpace(idResearch) ? null : idResearch.Split(',').Where(id => int.TryParse(id, out _)).Select(int.Parse).ToList();
            var tags = await _tagService.GetTags(limit, offset, expand.Split(',').ToList(), idList);
            var CountList = await _tagService.GetTagsCount();
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers","X-Total-Count");
            return Ok(tags);
        }

        [HttpGet("{id_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedTagDto>> GetTagById([FromRoute] int id_tag, [FromQuery, SwaggerParameter(Description = "Fields to expand. Possible values: 'stores_tags', 'items_tags', 'boxs_tags'. Multiple values can be specified by separating them with ','. Default: \"\"")] string expand = "")
        {
            var tag = await _tagService.GetTagById(id_tag, expand.Split(',').ToList());
            return Ok(tag);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadTagDto>> CreateTag([FromBody] CreateTagDto tag)
        {
            var newTag = await _tagService.CreateTag(tag);
            return CreatedAtAction(nameof(GetTagById), new { id_tag = newTag.id_tag }, newTag);
        }
        
        [HttpPost("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkTagDto>> CreateBulkTag([FromBody] List<CreateTagDto> tag)
        {
            var newTag = await _tagService.CreateBulkTag(tag);
            return Ok(newTag);
        }

        [HttpPut("{id_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadTagDto>> UpdateTag([FromRoute] int id_tag, [FromBody] UpdateTagDto tag)
        {
            var tagToUpdate = await _tagService.UpdateTag(id_tag, tag);
            return Ok(tagToUpdate);
        }
        
        [HttpDelete("{id_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteTag([FromRoute] int id_tag)
        {
            await _tagService.DeleteTag(id_tag);
            return NoContent();
        }
    }
}