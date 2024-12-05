using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.BoxTagService;

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
        public async Task<ActionResult<IEnumerable<ReadBoxTagDto>>> GetBoxsTagsByTagId([FromRoute] int id_tag, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var boxTags = await _boxTagService.GetBoxsTagsByTagId(id_tag, limit, offset);
            var CountList = await _boxTagService.GetBoxsTagsCountByTagId(id_tag);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers","X-Total-Count");
            return Ok(boxTags);
        }

        [HttpGet("{id_box}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBoxTagDto>> GetBoxTagById([FromRoute] int id_tag, [FromRoute] int id_box)
        {
            var boxTag = await _boxTagService.GetBoxTagById(id_box, id_tag);
            return Ok(boxTag);
        }
        
        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBoxTagDto>> CreateBoxsTag([FromRoute] int id_tag, [FromBody] int[] boxs)
        {
            var boxTags = await _boxTagService.CreateBoxTags(null, id_tag, null, boxs);
            return Ok(boxTags);
        }

        [HttpPost("{id_box}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBoxTagDto>> CreateBoxTag([FromRoute] int id_tag, [FromRoute] int id_box)
        {
            var boxTagDto = new CreateBoxTagDto
            {
                id_tag = id_tag,
                id_box = id_box
            };
            var boxTag = await _boxTagService.CreateBoxTag(boxTagDto);
            return CreatedAtAction(nameof(GetBoxTagById), new { id_tag = boxTag.id_tag, id_box = boxTag.id_box }, boxTag);
        }

        [HttpDelete("{id_box}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteBoxTag([FromRoute] int id_tag, [FromRoute] int id_box)
        {
            await _boxTagService.DeleteBoxTag(id_box, id_tag);
            return NoContent();
        }
    }
}