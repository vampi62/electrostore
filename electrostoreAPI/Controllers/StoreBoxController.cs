using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.BoxService;
using electrostore.Services.LedService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/store/{id_store}/box")]

    public class StoreBoxController : ControllerBase
    {
        private readonly IBoxService _boxService;
        private readonly ILedService _ledService;

        public StoreBoxController(IBoxService boxService, ILedService ledService)
        {
            _boxService = boxService;
            _ledService = ledService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadBoxDto>>> GetBoxsByStoreId([FromRoute] int id_store, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var boxs = await _boxService.GetBoxsByStoreId(id_store, limit, offset);
            return Ok(boxs);
        }

        [HttpGet("{id_box}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBoxDto>> GetBoxById([FromRoute] int id_store, [FromRoute] int id_box)
        {
            var box = await _boxService.GetBoxById(id_box, id_store);
            return Ok(box);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
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
            return CreatedAtAction(nameof(GetBoxById), new { id_store = box.id_store, id_box = box.id_box }, box);
        }

        [HttpPut("{id_box}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBoxDto>> UpdateBox([FromRoute] int id_store, [FromRoute] int id_box, [FromBody] UpdateBoxByStoreDto boxDto)
        {
            var boxDtoFull = new UpdateBoxDto
            {
                xstart_box = boxDto.xstart_box,
                ystart_box = boxDto.ystart_box,
                xend_box = boxDto.xend_box,
                yend_box = boxDto.yend_box
            };
            var box = await _boxService.UpdateBox(id_box, boxDtoFull, id_store);
            return Ok(box);
        }

        [HttpDelete("{id_box}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteBox([FromRoute] int id_store, [FromRoute] int id_box)
        {
            await _boxService.DeleteBox(id_box, id_store);
            return NoContent();
        }

        [HttpPost("{id_box}/show")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBoxDto>> showLedBox([FromRoute] int id_store, [FromRoute] int id_box, [FromQuery] int red, [FromQuery] int green, [FromQuery] int blue, [FromQuery] int timeshow, [FromQuery] int animation)
        {
            var box = await _boxService.GetBoxById(id_box, id_store);
            var ledsDB = await _ledService.GetLedsByStoreIdAndPosition(box.id_store, box.xstart_box, box.xend_box, box.ystart_box, box.yend_box);
            await _ledService.ShowLeds(ledsDB, red, green, blue, timeshow, animation);
            return NoContent();
        }
    }
}