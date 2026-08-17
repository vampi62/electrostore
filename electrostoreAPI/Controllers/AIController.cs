using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.AIService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/ai")]

    public class AIController : ControllerBase
    {
        private readonly IAIService _aiService;

        public AIController(IAIService aiService)
        {
            _aiService = aiService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadAIDto>>> GetIA([FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to select list of ID to research in the base. Multiple values can be specified by separating them with ','.")] List<int>? idResearch = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'name_ia=like=example'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'name_ia,asc' or 'name_ia,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var ais = await _aiService.GetIA(limit, offset, rsqlDto, sortDto, idResearch);
            return Ok(ais);
        }

        [HttpGet("{id_ia}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadAIDto>> GetIAById([FromRoute] int id_ia)
        {
            var ai = await _aiService.GetIAById(id_ia);
            return Ok(ai);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadAIDto>> CreateIA([FromBody] CreateAIDto ai)
        {
            var newIA = await _aiService.CreateIA(ai);
            return CreatedAtAction(nameof(GetIAById), new { id_ia = newIA.id_ia }, newIA);
        }

        [HttpGet("{id_ia}/status")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<AIStatusDto>> GetTrainingStatus(int id_ia)
        {
            var aiStatus = await _aiService.GetIATrainingStatusById(id_ia);
            return Ok(aiStatus);
        }

        [HttpPost("{id_ia}/train")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> TrainIA([FromRoute] int id_ia)
        {
            await _aiService.StartIATrainById(id_ia);
            return NoContent();
        }

        [HttpPost("{id_ia}/detect")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PredictionOutput>> DetectItem([FromRoute] int id_ia, [FromForm] DetecDto img_to_scan)
        {
            var detection = await _aiService.IADetectItem(id_ia, img_to_scan);
            return Ok(detection);
        }

        [HttpPut("{id_ia}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadAIDto>> UpdateIA([FromRoute] int id_ia, [FromBody] UpdateAIDto ai)
        {
            var aiToUpdate = await _aiService.UpdateIA(id_ia, ai);
            return Ok(aiToUpdate);
        }

        [HttpDelete("{id_ia}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteIA([FromRoute] int id_ia)
        {
            await _aiService.DeleteIA(id_ia);
            return NoContent();
        }
    }
}