using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.BoxService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/store/{id_store}/box")]

    public class StoreBoxController : ControllerBase
    {
        private readonly IBoxService _boxService;

        public StoreBoxController(IBoxService boxService)
        {
            _boxService = boxService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReadBoxDto>>> GetBoxsByStoreId([FromRoute] int id_store, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var boxs = await _boxService.GetBoxsByStoreId(id_store, limit, offset);
            if (boxs.Result is BadRequestObjectResult)
            {
                return boxs.Result;
            }
            if (boxs.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(boxs.Value);
        }

        [HttpGet("{id_box}")]
        public async Task<ActionResult<ReadBoxDto>> GetBoxById([FromRoute] int id_store, [FromRoute] int id_box)
        {
            var box = await _boxService.GetBoxById(id_box, id_store);
            if (box.Result is BadRequestObjectResult)
            {
                return box.Result;
            }
            if (box.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(box.Value);
        }

        [HttpPost]
        public async Task<ActionResult<ReadBoxDto>> CreateBox([FromRoute] int id_store, [FromBody] CreateBoxByStoreDto boxDto)
        {
            var boxDtoFull = new CreateBoxDto
            {
                xstart_box = boxDto.xstart_box,
                ystart_box = boxDto.ystart_box,
                xend_box = boxDto.xend_box,
                yend_box = boxDto.yend_box,
                id_store = id_store
            };
            var box = await _boxService.CreateBox(boxDtoFull);
            if (box.Result is BadRequestObjectResult)
            {
                return box.Result;
            }
            if (box.Value == null)
            {
                return StatusCode(500);
            }
            return CreatedAtAction(nameof(GetBoxById), new { id_store = box.Value.id_store, id_box = box.Value.id_box }, box.Value);
        }

        [HttpPut("{id_box}")]
        public async Task<ActionResult<ReadBoxDto>> UpdateBox([FromRoute] int id_store, [FromRoute] int id_box, [FromBody] UpdateBoxDto boxDto)
        {
            var box = await _boxService.UpdateBox(id_box, boxDto, id_store);
            if (box.Result is BadRequestObjectResult)
            {
                return box.Result;
            }
            if (box.Value == null)
            {
                return StatusCode(500);
            }
            return CreatedAtAction(nameof(GetBoxById), new { id_store = box.Value.id_store, id_box = box.Value.id_box }, box.Value);
        }

        [HttpDelete("{id_box}")]
        public async Task<ActionResult> DeleteBox([FromRoute] int id_store, [FromRoute] int id_box)
        {
            await _boxService.DeleteBox(id_box, id_store);
            return NoContent();
        }
    }
}