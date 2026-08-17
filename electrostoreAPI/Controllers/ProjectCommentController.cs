using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.ProjectCommentService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/project/{id_project}/comment")]

    public class ProjectCommentController : ControllerBase
    {
        private readonly IProjectCommentService _projectCommentService;

        public ProjectCommentController(IProjectCommentService projectCommentService)
        {
            _projectCommentService = projectCommentService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedProjectCommentDto>>> GetProjectCommentsByProjectId([FromRoute] int id_project, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'project', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL filter. Example: 'content_project_comment=like=test'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string. Example: 'created_at,asc' or 'created_at,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var projectComments = await _projectCommentService.GetProjectCommentsByProjectId(id_project, limit, offset, rsqlDto, sortDto, expand);
            return Ok(projectComments);
        }

        [HttpGet("{id_project_comment}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedProjectCommentDto>> GetProjectCommentsById([FromRoute] int id_project, [FromRoute] int id_project_comment,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'project', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projectComment = await _projectCommentService.GetProjectCommentsById(id_project_comment, null, id_project, expand);
            return Ok(projectComment);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjectCommentDto>> AddProjectComment([FromRoute] int id_project, [FromBody] CreateProjectCommentByProjectDto projectCommentDto)
        {
            var projectCommentDtoFull = new CreateProjectCommentDto
            {
                id_project = id_project,
                id_user = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : throw new InvalidOperationException("User identifier not found."),
                content_project_comment = projectCommentDto.content_project_comment
            };
            var projectComment = await _projectCommentService.CreateProjectComment(projectCommentDtoFull);
            return CreatedAtAction(nameof(GetProjectCommentsById), new { id_project = projectComment.id_project, id_project_comment = projectComment.id_project_comment }, projectComment);
        }

        [HttpPut("{id_project_comment}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjectCommentDto>> UpdateProjectComment([FromRoute] int id_project, [FromRoute] int id_project_comment, [FromBody] UpdateProjectCommentDto projectCommentDto)
        {
            var projectComment = await _projectCommentService.UpdateProjectComment(id_project_comment, projectCommentDto, null, id_project);
            return Ok(projectComment);
        }

        [HttpDelete("{id_project_comment}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteProjectComment([FromRoute] int id_project, [FromRoute] int id_project_comment)
        {
            await _projectCommentService.DeleteProjectComment(id_project_comment, null, id_project);
            return NoContent();
        }
    }
}