using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.BoxTagService;
using Swashbuckle.AspNetCore.Annotations;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/tag/{id_tag}/box")]

    public class TagBoxController : ControllerBase
    {
        private readonly IBoxTagService _boxTagService;

        public TagBoxController(IBoxTagService boxTagService)
        {
            _boxTagService = boxTagService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadExtendedBoxTagDto>>> GetBoxsTagsByTagId([FromRoute] int id_tag, [FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'tag', 'box'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var boxTags = await _boxTagService.GetBoxsTagsByTagId(id_tag, limit, offset, expand);
            var CountList = await _boxTagService.GetBoxsTagsCountByTagId(id_tag);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers","X-Total-Count");
            return Ok(boxTags);
        }

        [HttpGet("{id_box}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedBoxTagDto>> GetBoxTagById([FromRoute] int id_tag, [FromRoute] int id_box, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'tag', 'box'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var boxTag = await _boxTagService.GetBoxTagById(id_box, id_tag, expand);
            return Ok(boxTag);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBoxTagDto>> CreateBoxTag([FromRoute] int id_tag, [FromBody] CreateBoxTagByTagDto boxTagDto)
        {
            var boxTagDtoFull = new CreateBoxTagDto
            {
                id_tag = id_tag,
                id_box = boxTagDto.id_box
            };
            var boxTag = await _boxTagService.CreateBoxTag(boxTagDtoFull);
            return CreatedAtAction(nameof(GetBoxTagById), new { id_tag = boxTag.id_tag, id_box = boxTag.id_box }, boxTag);
        }

        [HttpPost("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkBoxTagDto>> CreateBulkBoxTag([FromRoute] int id_tag, [FromBody] List<CreateBoxTagByTagDto> boxTagsDto)
        {
            var boxTagsDtoFull = boxTagsDto.Select(boxTagDto => new CreateBoxTagDto
            {
                id_tag = id_tag,
                id_box = boxTagDto.id_box
            }).ToList();
            var boxTags = await _boxTagService.CreateBulkBoxTag(boxTagsDtoFull);
            return Ok(boxTags);
        }

        [HttpDelete("{id_box}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteBoxTag([FromRoute] int id_tag, [FromRoute] int id_box)
        {
            await _boxTagService.DeleteBoxTag(id_box, id_tag);
            return NoContent();
        }

        [HttpDelete("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkBoxTagDto>> DeleteBulkBoxTag([FromRoute] int id_tag, [FromBody] List<int> id_boxs)
        {
            var itemTagsDtoFull = id_boxs.Select(id_item => new CreateBoxTagDto
            {
                id_box = id_item,
                id_tag = id_tag
            }).ToList();
            var boxTags = await _boxTagService.DeleteBulkItemTag(itemTagsDtoFull);
            return Ok(boxTags);
        }
    }
}