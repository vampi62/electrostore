using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.LedService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/led")]

    public class LedController : ControllerBase
    {
        private readonly ILedService _ledService;

        public LedController(ILedService ledService)
        {
            _ledService = ledService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReadLedDto>>> GetLeds([FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var leds = await _ledService.GetLeds(limit, offset);
            return Ok(leds);
        }

        [HttpGet("{id_led}")]
        public async Task<ActionResult<ReadLedDto>> GetLedById([FromRoute] int id_led)
        {
            var led = await _ledService.GetLedById(id_led);
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
        public async Task<ActionResult<ReadLedDto>> AddLed([FromBody] CreateLedDto ledDto)
        {
            var led = await _ledService.CreateLed(ledDto);
            if (led.Result is BadRequestObjectResult)
            {
                return led.Result;
            }
            if (led.Value == null)
            {
                return StatusCode(500);
            }
            return CreatedAtAction(nameof(GetLedById), new { id_led = led.Value.id_led }, led.Value);
        }

        [HttpPost("{id_led}/show")]
        public async Task<ActionResult<ReadBoxDto>> showLedBox([FromRoute] int id_led, [FromQuery] int red, [FromQuery] int green, [FromQuery] int blue, [FromQuery] int timeshow, [FromQuery] int animation)
        {
            var ledDB = await _ledService.GetLedById(id_led);
            if (ledDB.Result is BadRequestObjectResult)
            {
                return ledDB.Result;
            }
            if (ledDB.Value == null)
            {
                return StatusCode(500);
            }
            await _ledService.ShowLed(ledDB.Value, red, green, blue, timeshow, animation);
            return NoContent();
        }

        [HttpPut("{id_led}")]
        public async Task<ActionResult<ReadLedDto>> UpdateLed([FromRoute] int id_led, [FromBody] UpdateLedDto ledDto)
        {
            var led = await _ledService.UpdateLed(id_led, ledDto);
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
        public async Task<ActionResult> DeleteLed([FromRoute] int id_led)
        {
            await _ledService.DeleteLed(id_led);
            return NoContent();
        }
    }
}