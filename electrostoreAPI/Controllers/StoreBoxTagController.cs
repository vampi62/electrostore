using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.BoxTagService;
using Swashbuckle.AspNetCore.Annotations;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/store/{id_store}/box/{id_box}/tag")]

    public class StoreBoxTagController : ControllerBase
    {
        private readonly IBoxTagService _boxTagService;

        public StoreBoxTagController(IBoxTagService boxTagService)
        {
            _boxTagService = boxTagService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadExtendedBoxTagDto>>> GetBoxsTagsByBoxId([FromRoute] int id_store, [FromRoute] int id_box, [FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'tag', 'box'. Multiple values can be specified by separating them with ','.")] string? expand = null)
        {
            await _boxTagService.CheckIfStoreExists(id_store,id_box);
            var boxsTags = await _boxTagService.GetBoxsTagsByBoxId(id_box, limit, offset, expand?.Split(',').ToList());
            var CountList = await _boxTagService.GetBoxsTagsCountByBoxId(id_box);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers","X-Total-Count");
            return Ok(boxsTags);
        }

        [HttpGet("{id_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedBoxTagDto>> GetBoxTagById([FromRoute] int id_store, [FromRoute] int id_box, [FromRoute] int id_tag, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'tag', 'box'. Multiple values can be specified by separating them with ','.")] string? expand = null)
        {
            await _boxTagService.CheckIfStoreExists(id_store,id_box);
            var boxTag = await _boxTagService.GetBoxTagById(id_box, id_tag, expand?.Split(',').ToList());
            return Ok(boxTag);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBoxTagDto>> CreateBoxTag([FromRoute] int id_store, [FromRoute] int id_box, [FromBody] CreateBoxTagByBoxDto boxTagDto)
        {
            await _boxTagService.CheckIfStoreExists(id_store,id_box);
            var boxTagDtoFull = new CreateBoxTagDto
            {
                id_box = id_box,
                id_tag = boxTagDto.id_tag
            };
            var boxTag = await _boxTagService.CreateBoxTag(boxTagDtoFull);
            return CreatedAtAction(nameof(GetBoxTagById), new { id_box = boxTag.id_box, id_tag = boxTag.id_tag }, boxTag);
        }

        [HttpPost("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkBoxTagDto>> CreateBulkBoxTag([FromRoute] int id_store, [FromRoute] int id_box, [FromBody] List<CreateBoxTagByBoxDto> boxTagsDto)
        {
            await _boxTagService.CheckIfStoreExists(id_store,id_box);
            var boxTagsDtoFull = boxTagsDto.Select(boxTagDto => new CreateBoxTagDto
            {
                id_box = id_box,
                id_tag = boxTagDto.id_tag
            }).ToList();
            var boxTags = await _boxTagService.CreateBulkBoxTag(boxTagsDtoFull);
            return Ok(boxTags);
        }

        [HttpDelete("{id_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteBoxTag([FromRoute] int id_store, [FromRoute] int id_box, [FromRoute] int id_tag)
        {
            await _boxTagService.CheckIfStoreExists(id_store,id_box);
            await _boxTagService.DeleteBoxTag(id_box, id_tag);
            return NoContent();
        }

        [HttpDelete("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkBoxTagDto>> DeleteBulkBoxTag([FromRoute] int id_box, [FromBody] List<int> id_tags)
        {
            var itemTagsDtoFull = id_tags.Select(id_item => new CreateBoxTagDto
            {
                id_box = id_box,
                id_tag = id_item
            }).ToList();
            var boxTags = await _boxTagService.DeleteBulkItemTag(itemTagsDtoFull);
            return Ok(boxTags);
        }
    }
}