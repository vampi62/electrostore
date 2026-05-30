using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.CronJobService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/cronjob")]
    public class CronJobController : ControllerBase
    {
        private readonly ICronJobService _cronJobService;

        public CronJobController(ICronJobService cronJobService)
        {
            _cronJobService = cronJobService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadCronJobDto>>> GetCronJobs(
            [FromQuery] int limit = 100,
            [FromQuery] int offset = 0,
            [FromQuery, SwaggerParameter(Description = "(Optional) Fields to select list of ID to research in the base. Multiple values can be specified by separating them with ','.")] List<int>? idResearch = null,
            [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'name_cronjob=like=example'.")] string? filter = null,
            [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'name_cronjob,asc' or 'name_cronjob,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var cronJobs = await _cronJobService.GetCronJobs(limit, offset, rsqlDto, sortDto, idResearch);
            return Ok(cronJobs);
        }

        [HttpGet("{id_cronjob}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCronJobDto>> GetCronJobById([FromRoute] int id_cronjob)
        {
            var cronJob = await _cronJobService.GetCronJobById(id_cronjob);
            return Ok(cronJob);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCronJobDto>> CreateCronJob([FromBody] CreateCronJobDto cronJobDto)
        {
            var cronJob = await _cronJobService.CreateCronJob(cronJobDto);
            return CreatedAtAction(nameof(GetCronJobById), new { id_cronjob = cronJob.id_cronjob }, cronJob);
        }

        [HttpPut("{id_cronjob}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCronJobDto>> UpdateCronJob([FromRoute] int id_cronjob, [FromBody] UpdateCronJobDto cronJobDto)
        {
            var cronJob = await _cronJobService.UpdateCronJob(id_cronjob, cronJobDto);
            return Ok(cronJob);
        }

        [HttpDelete("{id_cronjob}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteCronJob([FromRoute] int id_cronjob)
        {
            await _cronJobService.DeleteCronJob(id_cronjob);
            return NoContent();
        }
    }
}
