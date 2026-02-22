using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.TagService;
using Swashbuckle.AspNetCore.Annotations;
using electrostore.Extensions;

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
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedTagDto>>> GetTags([FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'stores_tags', 'items_tags', 'boxs_tags'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to select list of ID to research in the base. Multiple values can be specified by separating them with ','.")] List<int>? idResearch = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'nom_tag=like=example'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'nom_tag,asc' or 'nom_tag,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var tags = await _tagService.GetTags(limit, offset, rsqlDto, sortDto, expand, idResearch);
            return Ok(tags);
        }

        [HttpGet("{id_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedTagDto>> GetTagById([FromRoute] int id_tag,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'stores_tags', 'items_tags', 'boxs_tags'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var tag = await _tagService.GetTagById(id_tag, expand);
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