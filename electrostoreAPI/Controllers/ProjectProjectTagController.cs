using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.ProjectProjectTagService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/project/{id_project}/project-tag")]

    public class ProjectProjectTagController : ControllerBase
    {
        private readonly IProjectProjectTagService _projectProjectTagService;

        public ProjectProjectTagController(IProjectProjectTagService projectProjectTagService)
        {
            _projectProjectTagService = projectProjectTagService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedProjectProjectTagDto>>> GetProjectsProjectTagsByProjectId([FromRoute] int id_project, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'project_tag', 'project'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL filter. Example: 'id_project_tag==5'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string. Example: 'id_project_tag,asc' or 'id_project_tag,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var projectProjectTags = await _projectProjectTagService.GetProjectsProjectTagsByProjectId(id_project, limit, offset, rsqlDto, sortDto, expand);
            return Ok(projectProjectTags);
        }
        
        [HttpGet("{id_project_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedProjectProjectTagDto>> GetProjectProjectTagById([FromRoute] int id_project, [FromRoute] int id_project_tag,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'project_tag', 'project'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projectProjectTag = await _projectProjectTagService.GetProjectProjectTagById(id_project, id_project_tag, expand);
            return Ok(projectProjectTag);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjectProjectTagDto>> CreateProjectProjectTag([FromRoute] int id_project, [FromBody] CreateProjectProjectTagByProjectDto projectProjectTagDto)
        {
            var projectProjectTagFull = new CreateProjectProjectTagDto
            {
                id_project = id_project,
                id_project_tag = projectProjectTagDto.id_project_tag
            };
            var newProjectProjectTag = await _projectProjectTagService.CreateProjectProjectTag(projectProjectTagFull);
            return CreatedAtAction(nameof(GetProjectProjectTagById), new { id_project = newProjectProjectTag.id_project, id_project_tag = newProjectProjectTag.id_project_tag }, newProjectProjectTag);
        }

        [HttpPost("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkProjectProjectTagDto>> CreateBulkProjectProjectTag([FromRoute] int id_project, [FromBody] List<CreateProjectProjectTagByProjectDto> projectProjectTagsDto)
        {
            var projectProjectTagsDtoFull = projectProjectTagsDto.Select(projectProjectTagDto => new CreateProjectProjectTagDto
            {
                id_project = id_project,
                id_project_tag = projectProjectTagDto.id_project_tag
            }).ToList();
            var projectProjectTags = await _projectProjectTagService.CreateBulkProjectProjectTag(projectProjectTagsDtoFull);
            return Ok(projectProjectTags);
        }

        [HttpDelete("{id_project_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteProjectProjectTag([FromRoute] int id_project, [FromRoute] int id_project_tag)
        {
            await _projectProjectTagService.DeleteProjectProjectTag(id_project, id_project_tag);
            return NoContent();
        }

        [HttpDelete("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkProjectProjectTagDto>> DeleteBulkProjectProjectTag([FromRoute] int id_project, [FromBody] List<int> id_project_tags)
        {
            var projectProjectTagsDtoFull = id_project_tags.Select(id_project_tag => new CreateProjectProjectTagDto
            {
                id_project = id_project,
                id_project_tag = id_project_tag
            }).ToList();
            var projectProjectTags = await _projectProjectTagService.DeleteBulkProjectProjectTag(projectProjectTagsDtoFull);
            return Ok(projectProjectTags);
        }
    }
}