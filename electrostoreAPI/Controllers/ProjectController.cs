using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.ProjectService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/project")]

    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedProjectDto>>> GetProjects([FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'project_comments', 'project_documents', 'project_items', 'project_tags', 'project_status_history', 'project_steps'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to select list of ID to research in the base. Multiple values can be specified by separating them with ','.")] List<int>? idResearch = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'name_project=like=example'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'name_project,asc' or 'name_project,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var projects = await _projectService.GetProjects(limit, offset, rsqlDto, sortDto, expand, idResearch);
            return Ok(projects);
        }

        [HttpGet("{id_project}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedProjectDto>> GetProjectById([FromRoute] int id_project,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'project_comments', 'project_documents', 'project_items', 'project_tags', 'project_status_history', 'project_steps'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var project = await _projectService.GetProjectById(id_project, expand);
            return Ok(project);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjectDto>> CreateProject([FromBody] CreateProjectDto projectDto)
        {
            var project = await _projectService.CreateProject(projectDto);
            return CreatedAtAction(nameof(GetProjectById), new { id_project = project.id_project }, project);
        }

        [HttpPut("{id_project}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjectDto>> UpdateProject([FromRoute] int id_project, [FromBody] UpdateProjectDto projectDto)
        {
            var project = await _projectService.UpdateProject(id_project, projectDto);
            return Ok(project);
        }

        [HttpDelete("{id_project}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteProject([FromRoute] int id_project)
        {
            await _projectService.DeleteProject(id_project);
            return NoContent();
        }
    }
}