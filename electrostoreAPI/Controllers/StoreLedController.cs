using Microsoft.AspNetCore.Mvc;
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
        public async Task<ActionResult<IEnumerable<ReadLedDto>>> GetLedsByStoreId([FromRoute] int id_store, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var leds = await _ledService.GetLedsByStoreId(id_store, limit, offset);
            if (leds.Result is BadRequestObjectResult)
            {
                return leds.Result;
            }
            if (leds.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(leds.Value);
        }

        [HttpGet("{id_led}")]
        public async Task<ActionResult<ReadLedDto>> GetLedById([FromRoute] int id_store, [FromRoute] int id_led)
        {
            var led = await _ledService.GetLedById(id_led, id_store);
            if (led.Result is BadRequestObjectResult)
            {
                return led.Result;
            }
            if (led.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(led.Value);
        }

        [HttpPost]
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
            if (led.Result is BadRequestObjectResult)
            {
                return led.Result;
            }
            if (led.Value == null)
            {
                return StatusCode(500);
            }
            return CreatedAtAction(nameof(GetLedById), new { id_store = led.Value.id_store, id_led = led.Value.id_led }, led.Value);
        }

        [HttpPut("{id_led}")]
        public async Task<ActionResult<ReadLedDto>> UpdateLed([FromRoute] int id_store, [FromRoute] int id_led, [FromBody] UpdateLedByStoreDto ledDto)
        {
            var ledDtoFull = new UpdateLedDto
            {
                x_led = ledDto.x_led,
                y_led = ledDto.y_led,
                mqtt_led_id = ledDto.mqtt_led_id
            };
            var led = await _ledService.UpdateLed(id_led, ledDtoFull, id_store);
            if (led.Result is BadRequestObjectResult)
            {
                return led.Result;
            }
            if (led.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(led.Value);
        }

        [HttpDelete("{id_led}")]
        public async Task<ActionResult> DeleteLed([FromRoute] int id_store, [FromRoute] int id_led)
        {
            await _ledService.DeleteLed(id_led, id_store);
            return NoContent();
        }
    }
}