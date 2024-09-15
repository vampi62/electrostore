using Microsoft.AspNetCore.Mvc;
using electrostore.Dto;
using electrostore.Services.IAService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/ia")]

    public class IAController : ControllerBase
    {
        private readonly IIAService _iaService;

        public IAController(IIAService iaService)
        {
            _iaService = iaService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReadIADto>>> GetIA([FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var ias = await _iaService.GetIA(limit, offset);
            return Ok(ias);
        }

        [HttpGet("{id_ia}")]
        public async Task<ActionResult<ReadIADto>> GetIAById([FromRoute] int id_ia)
        {
            var ia = await _iaService.GetIAById(id_ia);
            if (ia.Result is BadRequestObjectResult)
            {
                return ia.Result;
            }
            if (ia.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(ia.Value);
        }

        [HttpPost]
        public async Task<ActionResult<ReadIADto>> CreateIA([FromBody] CreateIADto ia)
        {
            var newIA = await _iaService.CreateIA(ia);
            return CreatedAtAction(nameof(GetIAById), new { id_ia = newIA.id_ia }, newIA);
        }

        [HttpGet("{id_ia}/status")]
        public async Task<IActionResult> GetTrainingStatus(string id_ia)
        {
            var status = await _iaService.GetTrainingStatus(id_ia);
            if (status != null)
            {
                return Ok(status);
            }

            return NotFound("Aucun entraînement en cours pour cet ID.");
        }

        [HttpPost("{id_ia}/train")]
        public async Task<ActionResult<bool>> TrainIA([FromRoute] int id_ia)
        {
            if (User != null)
            {
                if (!User.IsInRole("admin"))
                {
                    return Unauthorized(new { message = "You are not allowed to train an IA" });
                }
            }
            var result = await _iaService.TrainIA(id_ia);
            if (!result.TrainStarted)
            {
                return Conflict(result.msg);
            }
            return Ok(result.msg);
        }

        [HttpPost("{id_ia}/detect")]
        public async Task<ActionResult<ReadItemDto>> DetectItem([FromRoute] int id_ia, [FromForm] DetecDto img_to_scan)
        {
            var item = await _iaService.DetectItem(id_ia, img_to_scan.img_file);
            if (item.Result is BadRequestObjectResult)
            {
                return item.Result;
            }
            if (item.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(item.Value);
        }

        [HttpPut("{id_ia}")]
        public async Task<ActionResult<ReadIADto>> UpdateIA([FromRoute] int id_ia, [FromBody] UpdateIADto ia)
        {
            var iaToUpdate = await _iaService.UpdateIA(id_ia, ia);
            if (iaToUpdate.Result is BadRequestObjectResult)
            {
                return iaToUpdate.Result;
            }
            if (iaToUpdate.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(iaToUpdate.Value);
        }

        [HttpDelete("{id_ia}")]
        public async Task<ActionResult> DeleteIA([FromRoute] int id_ia)
        {
            await _iaService.DeleteIA(id_ia);
            return NoContent();
        }
    }
}