using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.BoxTagService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/box/{id_box}/tag")]

    public class BoxTagController : ControllerBase
    {
        private readonly IBoxTagService _boxTagService;

        public BoxTagController(IBoxTagService boxTagService)
        {
            _boxTagService = boxTagService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReadBoxTagDto>>> GetBoxsTagsByBoxId([FromRoute] int id_box, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var boxsTags = await _boxTagService.GetBoxsTagsByBoxId(id_box, limit, offset);
            if (boxsTags.Result is BadRequestObjectResult)
            {
                return boxsTags.Result;
            }
            if (boxsTags.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(boxsTags.Value);
        }

        [HttpGet("{id_tag}")]
        public async Task<ActionResult<ReadBoxTagDto>> GetBoxTagById([FromRoute] int id_box, [FromRoute] int id_tag)
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

        [HttpPost]
        public async Task<ActionResult<ReadBoxTagDto>> CreateBoxTag([FromRoute] int id_box, [FromRoute] int id_tag)
        {
            var boxTagDto = new CreateBoxTagDto
            {
                id_box = id_box,
                id_tag = id_tag
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
            return CreatedAtAction(nameof(GetBoxTagById), new { id_box = boxTag.Value.id_box, id_tag = boxTag.Value.id_tag }, boxTag.Value);
        }

        [HttpDelete("{id_tag}")]
        public async Task<ActionResult> DeleteBoxTag([FromRoute] int id_box, [FromRoute] int id_tag)
        {
            await _boxTagService.DeleteBoxTag(id_box, id_tag);
            return NoContent();
        }
    }
}