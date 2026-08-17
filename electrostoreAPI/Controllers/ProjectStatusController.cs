using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.ProjectStatusService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/project/{id_project}/status-history")]

    public class ProjectStatusController : ControllerBase
    {
        private readonly IProjectStatusService _projectStatusService;

        public ProjectStatusController(IProjectStatusService projectStatusService)
        {
            _projectStatusService = projectStatusService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedProjectStatusDto>>> GetProjectStatusByProjectId([FromRoute] int id_project, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL filter. Example: 'status_project==0'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string. Example: 'created_at,asc' or 'created_at,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var projectStatus = await _projectStatusService.GetProjectStatusByProjectId(id_project, limit, offset, rsqlDto, sortDto);
            return Ok(projectStatus);
        }

        [HttpGet("{id_project_status}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedProjectStatusDto>> GetProjectStatusById([FromRoute] int id_project, [FromRoute] int id_project_status)
        {
            var projectStatus = await _projectStatusService.GetProjectStatusById(id_project_status, id_project);
            return Ok(projectStatus);
        }
    }
}