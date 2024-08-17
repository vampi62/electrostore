using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.BoxService;
using electrostore.Services.LedService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/box")]

    public class BoxController : ControllerBase
    {
        private readonly IBoxService _boxService;
        private readonly ILedService _ledService;

        public BoxController(IBoxService boxService, ILedService ledService)
        {
            _boxService = boxService;
            _ledService = ledService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReadBoxDto>>> GetBoxs([FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var boxs = await _boxService.GetBoxs(limit, offset);
            return Ok(boxs);
        }

        [HttpGet("{id_box}")]
        public async Task<ActionResult<ReadBoxDto>> GetBoxById([FromRoute] int id_box)
        {
            var box = await _boxService.GetBoxById(id_box);
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
        public async Task<ActionResult<ReadBoxDto>> CreateBox([FromBody] CreateBoxDto boxDto)
        {
            var box = await _boxService.CreateBox(boxDto);
            if (box.Result is BadRequestObjectResult)
            {
                return box.Result;
            }
            if (box.Value == null)
            {
                return StatusCode(500);
            }
            return CreatedAtAction(nameof(GetBoxById), new { id_box = box.Value.id_box }, box.Value);
        }

        [HttpPost("{id_box}/show")]
        public async Task<ActionResult<ReadBoxDto>> showLedBox([FromRoute] int id_box, [FromQuery] int red, [FromQuery] int green, [FromQuery] int blue, [FromQuery] int timeshow, [FromQuery] int animation)
        {
            var box = await _boxService.GetBoxById(id_box);
            if (box.Result is BadRequestObjectResult)
            {
                return box.Result;
            }
            if (box.Value == null)
            {
                return StatusCode(500);
            }
            var ledsDB = await _ledService.GetLedsByStoreIdAndPosition(box.Value.id_store, box.Value.xstart_box, box.Value.xend_box, box.Value.ystart_box, box.Value.yend_box);
            if (ledsDB.Result is BadRequestObjectResult)
            {
                return ledsDB.Result;
            }
            if (ledsDB.Value == null)
            {
                return StatusCode(500);
            }
            await _ledService.ShowLeds(ledsDB.Value, red, green, blue, timeshow, animation);
            return NoContent();
        }

        [HttpPut("{id_box}")]
        public async Task<ActionResult<ReadBoxDto>> UpdateBox([FromRoute] int id_box, [FromBody] UpdateBoxDto boxDto)
        {
            var box = await _boxService.UpdateBox(id_box,boxDto);
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

        [HttpDelete("{id_box}")]
        public async Task<ActionResult> DeleteBox([FromRoute] int id_box)
        {
            await _boxService.DeleteBox(id_box);
            return NoContent();
        }
    }
}