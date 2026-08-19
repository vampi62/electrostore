using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.ProjectStepService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/project/{id_project}/step")]

    public class ProjectStepController : ControllerBase
    {
        private readonly IProjectStepService _projectStepService;

        public ProjectStepController(IProjectStepService projectStepService)
        {
            _projectStepService = projectStepService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedProjectStepDto>>> GetProjectStepsByProjectId([FromRoute] int id_project, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'project'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL filter. Example: 'status_project_step==0'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string. Example: 'order_project_step,asc' or 'order_project_step,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var projectSteps = await _projectStepService.GetProjectStepsByProjectId(id_project, limit, offset, rsqlDto, sortDto, expand);
            return Ok(projectSteps);
        }

        [HttpGet("{id_project_step}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedProjectStepDto>> GetProjectStepById([FromRoute] int id_project, [FromRoute] int id_project_step,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'project'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projectStep = await _projectStepService.GetProjectStepById(id_project_step, id_project, expand);
            return Ok(projectStep);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjectStepDto>> CreateProjectStep([FromRoute] int id_project, [FromBody] CreateProjectStepByProjectDto projectStepDto)
        {
            var projectStepDtoFull = new CreateProjectStepDto
            {
                id_project = id_project,
                name_project_step = projectStepDto.name_project_step,
                description_project_step = projectStepDto.description_project_step ?? string.Empty,
                status_project_step = projectStepDto.status_project_step,
                order_project_step = projectStepDto.order_project_step ?? 0,
                planned_start_project_step = projectStepDto.planned_start_project_step,
                planned_end_project_step = projectStepDto.planned_end_project_step,
                actual_start_project_step = projectStepDto.actual_start_project_step,
                actual_end_project_step = projectStepDto.actual_end_project_step
            };
            var projectStep = await _projectStepService.CreateProjectStep(projectStepDtoFull);
            return CreatedAtAction(nameof(GetProjectStepById), new { id_project = projectStep.id_project, id_project_step = projectStep.id_project_step }, projectStep);
        }

        [HttpPut("{id_project_step}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjectStepDto>> UpdateProjectStep([FromRoute] int id_project, [FromRoute] int id_project_step, [FromBody] UpdateProjectStepDto projectStepDto)
        {
            var projectStep = await _projectStepService.UpdateProjectStep(id_project_step, projectStepDto, id_project);
            return Ok(projectStep);
        }

        [HttpDelete("{id_project_step}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteProjectStep([FromRoute] int id_project, [FromRoute] int id_project_step)
        {
            await _projectStepService.DeleteProjectStep(id_project_step, id_project);
            return NoContent();
        }
    }
}
