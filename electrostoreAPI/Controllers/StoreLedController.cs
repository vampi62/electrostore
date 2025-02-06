using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.LedService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/store/{id_store}/led")]

    public class StoreLedController : ControllerBase
    {
        private readonly ILedService _ledService;

        public StoreLedController(ILedService ledService)
        {
            _ledService = ledService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadLedDto>>> GetLedsByStoreId([FromRoute] int id_store, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var leds = await _ledService.GetLedsByStoreId(id_store, limit, offset);
            var CountList = await _ledService.GetLedsCountByStoreId(id_store);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers","X-Total-Count");
            return Ok(leds);
        }

        [HttpGet("{id_led}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadLedDto>> GetLedById([FromRoute] int id_store, [FromRoute] int id_led)
        {
            var led = await _ledService.GetLedById(id_led, id_store);
            return Ok(led);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadLedDto>> CreateLed([FromRoute] int id_store, [FromBody] CreateLedByStoreDto ledDto)
        {
            var ledDtoFull = new CreateLedDto
            {
                x_led = ledDto.x_led,
                y_led = ledDto.y_led,
                id_store = id_store,
                mqtt_led_id = ledDto.mqtt_led_id
            };
            var led = await _ledService.CreateLed(ledDtoFull);
            return CreatedAtAction(nameof(GetLedById), new { id_store = led.id_store, id_led = led.id_led }, led);
        }

        [HttpPost("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkLedDto>> CreateBulkLed([FromRoute] int id_store, [FromBody] List<CreateLedByStoreDto> ledsDto)
        {
            var ledsDtoFull = ledsDto.Select(ledDto => new CreateLedDto
            {
                x_led = ledDto.x_led,
                y_led = ledDto.y_led,
                id_store = id_store,
                mqtt_led_id = ledDto.mqtt_led_id
            }).ToList();
            var leds = await _ledService.CreateBulkLed(ledsDtoFull);
            return Ok(leds);
        }

        [HttpPut("{id_led}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadLedDto>> UpdateLed([FromRoute] int id_store, [FromRoute] int id_led, [FromBody] UpdateLedByStoreDto ledDto)
        {
            var ledDtoFull = new UpdateLedDto
            {
                x_led = ledDto.x_led,
                y_led = ledDto.y_led,
                mqtt_led_id = ledDto.mqtt_led_id
            };
            var led = await _ledService.UpdateLed(id_led, ledDtoFull, id_store);
            return Ok(led);
        }
        
        [HttpPut("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkLedDto>> UpdateBulkLed([FromRoute] int id_store, [FromBody] List<UpdateBulkLedStoreDto> ledsDto)
        {
            var leds = await _ledService.UpdateBulkLed(ledsDto, id_store);
            return Ok(leds);
        }

        [HttpDelete("{id_led}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteLed([FromRoute] int id_store, [FromRoute] int id_led)
        {
            await _ledService.DeleteLed(id_led, id_store);
            return NoContent();
        }

        [HttpDelete("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkLedDto>> DeleteBulkLed([FromRoute] int id_store, [FromBody] List<int> ids)
        {
            var leds = await _ledService.DeleteBulkLed(ids, id_store);
            return Ok(leds);
        }

        [HttpPost("{id_led}/show")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBoxDto>> showLedBox([FromRoute] int id_store, [FromRoute] int id_led, [FromQuery] int red, [FromQuery] int green, [FromQuery] int blue, [FromQuery] int timeshow, [FromQuery] int animation)
        {
            var ledDB = await _ledService.GetLedById(id_led, id_store);
            await _ledService.ShowLed(ledDB, red, green, blue, timeshow, animation);
            return NoContent();
        }
    }
}