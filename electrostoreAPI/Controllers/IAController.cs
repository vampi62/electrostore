using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.IAService;
using Swashbuckle.AspNetCore.Annotations;

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
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadIADto>>> GetIA([FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to select list of ID to research in the base. Multiple values can be specified by separating them with ','.")] List<int>? idResearch = null)
        {
            var ias = await _iaService.GetIA(limit, offset, idResearch);
            var CountList = await _iaService.GetIACount();
            Response.Headers["X-Total-Count"] = CountList.ToString();
            Response.Headers.AccessControlExposeHeaders = "X-Total-Count";
            return Ok(ias);
        }

        [HttpGet("{id_ia}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadIADto>> GetIAById([FromRoute] int id_ia)
        {
            var ia = await _iaService.GetIAById(id_ia);
            return Ok(ia);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadIADto>> CreateIA([FromBody] CreateIADto ia)
        {
            var newIA = await _iaService.CreateIA(ia);
            return CreatedAtAction(nameof(GetIAById), new { id_ia = newIA.id_ia }, newIA);
        }

        [HttpGet("{id_ia}/status")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IAStatusDto>> GetTrainingStatus(int id_ia)
        {
            var IAStatus = await _iaService.GetIATrainingStatusById(id_ia);
            return Ok(IAStatus);
        }

        [HttpPost("{id_ia}/train")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> TrainIA([FromRoute] int id_ia)
        {
            await _iaService.StartIATrainById(id_ia);
            return NoContent();
        }

        [HttpPost("{id_ia}/detect")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PredictionOutput>> DetectItem([FromRoute] int id_ia, [FromForm] DetecDto img_to_scan)
        {
            var detection = await _iaService.IADetectItem(id_ia, img_to_scan);
            return Ok(detection);
        }

        [HttpPut("{id_ia}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadIADto>> UpdateIA([FromRoute] int id_ia, [FromBody] UpdateIADto ia)
        {
            var iaToUpdate = await _iaService.UpdateIA(id_ia, ia);
            return Ok(iaToUpdate);
        }

        [HttpDelete("{id_ia}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteIA([FromRoute] int id_ia)
        {
            await _iaService.DeleteIA(id_ia);
            return NoContent();
        }
    }
}