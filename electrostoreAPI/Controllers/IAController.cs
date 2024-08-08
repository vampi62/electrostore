using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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
            return Ok(ia);
        }

        [HttpPost]
        public async Task<ActionResult<ReadIADto>> CreateIA([FromBody] CreateIADto ia)
        {
            var newIA = await _iaService.CreateIA(ia);
            return CreatedAtAction(nameof(GetIAById), new { id_ia = newIA.id_ia }, newIA);
        }

        [HttpPost("{id_ia}/train")]
        public async Task<ActionResult<ReadIADto>> TrainIA([FromRoute] int id_ia)
        {
            //TODO : Train IA
            var ia = await _iaService.TrainIA(id_ia);
            return Ok(ia);
        }

        [HttpPost("{id_ia}/detect")]
        public async Task<ActionResult<ReadIADto>> DetectItem([FromRoute] int id_ia, [FromForm] IFormFile newFile) // 5MB max
        {
            //TODO : Detect IA
        }

        [HttpPut("{id_ia}")]
        public async Task<ActionResult<ReadIADto>> UpdateIA([FromRoute] int id_ia, [FromBody] UpdateIADto ia)
        {
            var iaToUpdate = await _iaService.UpdateIA(id_ia, ia);
            return Ok(iaToUpdate);
        }

        [HttpDelete("{id_ia}")]
        public async Task<ActionResult> DeleteIA([FromRoute] int id_ia)
        {
            await _iaService.DeleteIA(id_ia);
            return NoContent();
        }
    }
}