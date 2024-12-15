using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.BoxService;
using electrostore.Services.LedService;
using Swashbuckle.AspNetCore.Annotations;

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
        public async Task<ActionResult<IEnumerable<ReadExtendedBoxDto>>> GetBoxsByStoreId([FromRoute] int id_store, [FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery, SwaggerParameter(Description = "Fields to expand. Possible values: 'store', 'box_tags', 'item_boxs'. Multiple values can be specified by separating them with ','. Default: \"\"")] string expand = "")
        {
            var boxs = await _boxService.GetBoxsByStoreId(id_store, limit, offset, expand.Split(',').ToList());
            var CountList = await _boxService.GetBoxsCountByStoreId(id_store);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers","X-Total-Count");
            return Ok(boxs);
        }

        [HttpGet("{id_box}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedBoxDto>> GetBoxById([FromRoute] int id_store, [FromRoute] int id_box, [FromQuery, SwaggerParameter(Description = "Fields to expand. Possible values: 'store', 'box_tags', 'item_boxs'. Multiple values can be specified by separating them with ','. Default: \"\"")] string expand = "")
        {
            var box = await _boxService.GetBoxById(id_box, id_store, expand.Split(',').ToList());
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

        [HttpPost("bulk")]
		[Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkBoxDto>> CreateBulkBox([FromRoute] int id_store, [FromBody] List<CreateBoxByStoreDto> boxsDto)
        {
            var boxsDtoFull = boxsDto.Select(boxDto => new CreateBoxDto
            {
                xstart_box = boxDto.xstart_box,
                ystart_box = boxDto.ystart_box,
                xend_box = boxDto.xend_box,
                yend_box = boxDto.yend_box,
                id_store = id_store
            }).ToList();
            var boxs = await _boxService.CreateBulkBox(boxsDtoFull);
            return Ok(boxs);
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

        [HttpPut("bulk")]
		[Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkBoxDto>> UpdateBulkBox([FromRoute] int id_store, [FromBody] List<UpdateBuckBoxByStoreDto> boxsDto)
        {
            var boxs = await _boxService.UpdateBulkBox(boxsDto, id_store);
            return Ok(boxs);
        }

        [HttpDelete("{id_box}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteBox([FromRoute] int id_store, [FromRoute] int id_box)
        {
            await _boxService.DeleteBox(id_box, id_store);
            return NoContent();
        }

        [HttpDelete("bulk")]
		[Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkBoxDto>> DeleteBulkBox([FromRoute] int id_store, [FromBody] List<int> ids)
        {
            var boxs = await _boxService.DeleteBulkBox(ids, id_store);
            return Ok(boxs);
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