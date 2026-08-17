using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.ProjectTagService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/project-tag")]

    public class ProjectTagController : ControllerBase
    {
        private readonly IProjectTagService _projectTagService;

        public ProjectTagController(IProjectTagService projectTagService)
        {
            _projectTagService = projectTagService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedProjectTagDto>>> GetProjectTags([FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'project_tags'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to select list of ID to research in the base. Multiple values can be specified by separating them with ','.")] List<int>? idResearch = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'name_project_tag=like=example'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'id_project_tag,asc' or 'id_project_tag,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var projectTags = await _projectTagService.GetProjectTags(limit, offset, rsqlDto, sortDto, expand, idResearch);
            return Ok(projectTags);
        }

        [HttpGet("{id_project_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedTagDto>> GetProjectTagById([FromRoute] int id_project_tag,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'project_tags'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projectTags = await _projectTagService.GetProjectTagById(id_project_tag, expand);
            return Ok(projectTags);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadTagDto>> CreateProjectTag([FromBody] CreateProjectTagDto projectTag)
        {
            var newProjectTag = await _projectTagService.CreateProjectTag(projectTag);
            return CreatedAtAction(nameof(GetProjectTagById), new { id_project_tag = newProjectTag.id_project_tag }, newProjectTag);
        }
        
        [HttpPost("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkTagDto>> CreateBulkProjectTag([FromBody] List<CreateProjectTagDto> projectTag)
        {
            var newProjectTag = await _projectTagService.CreateBulkProjectTag(projectTag);
            return Ok(newProjectTag);
        }

        [HttpPut("{id_project_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadTagDto>> UpdateProjectTag([FromRoute] int id_project_tag, [FromBody] UpdateProjectTagDto projectTag)
        {
            var tagToUpdate = await _projectTagService.UpdateProjectTag(id_project_tag, projectTag);
            return Ok(tagToUpdate);
        }
        
        [HttpDelete("{id_project_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteProjectTag([FromRoute] int id_project_tag)
        {
            await _projectTagService.DeleteProjectTag(id_project_tag);
            return NoContent();
        }
    }
}