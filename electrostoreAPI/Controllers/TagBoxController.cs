using Microsoft.AspNetCore.Mvc;
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
        public async Task<ActionResult<IEnumerable<ReadBoxTagDto>>> GetBoxsTagsByTagId([FromRoute] int id_tag, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var boxTags = await _boxTagService.GetBoxsTagsByTagId(id_tag, limit, offset);
            if (boxTags.Result is BadRequestObjectResult)
            {
                return boxTags.Result;
            }
            if (boxTags.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(boxTags.Value);
        }

        [HttpGet("{id_box}")]
        public async Task<ActionResult<ReadBoxTagDto>> GetBoxTagById([FromRoute] int id_tag, [FromRoute] int id_box)
        {
            var boxTag = await _boxTagService.GetBoxTagById(id_box, id_tag);
            if (boxTag.Result is BadRequestObjectResult)
            {
                return boxTag.Result;
            }
            if (boxTag.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(boxTag.Value);
        }

        [HttpPost("{id_box}")]
        public async Task<ActionResult<ReadBoxTagDto>> CreateBoxTag([FromRoute] int id_tag, [FromRoute] int id_box)
        {
            var boxTagDto = new CreateBoxTagDto
            {
                id_tag = id_tag,
                id_box = id_box
            };
            var boxTag = await _boxTagService.CreateBoxTag(boxTagDto);
            if (boxTag.Result is BadRequestObjectResult)
            {
                return boxTag.Result;
            }
            if (boxTag.Value == null)
            {
                return StatusCode(500);
            }
            return CreatedAtAction(nameof(GetBoxTagById), new { id_tag = boxTag.Value.id_tag, id_box = boxTag.Value.id_box }, boxTag.Value);
        }

        [HttpDelete("{id_box}")]
        public async Task<ActionResult> DeleteBoxTag([FromRoute] int id_tag, [FromRoute] int id_box)
        {
            await _boxTagService.DeleteBoxTag(id_box, id_tag);
            return NoContent();
        }
    }
}