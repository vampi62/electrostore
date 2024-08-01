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

        [HttpGet("{id}")]
        public async Task<ActionResult<ReadIADto>> GetIAById([FromRoute] int id)
        {
            var ia = await _iaService.GetIAById(id);
            return Ok(ia);
        }

        [HttpPost]
        public async Task<ActionResult<ReadIADto>> CreateIA([FromBody] CreateIADto ia)
        {
            var newIA = await _iaService.CreateIA(ia);
            return CreatedAtAction(nameof(GetIAById), new { id = newIA.id_ia }, newIA);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ReadIADto>> UpdateIA([FromRoute] int id, [FromBody] UpdateIADto ia)
        {
            var iaToUpdate = await _iaService.UpdateIA(id, ia);
            return Ok(iaToUpdate);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteIA([FromRoute] int id)
        {
            await _iaService.DeleteIA(id);
            return NoContent();
        }
    }
}