using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.ProjectCommentService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/user/{id_user}/project_comment")]

    public class UserProjectCommentController : ControllerBase
    {
        private readonly IProjectCommentService _projectCommentService;

        public UserProjectCommentController(IProjectCommentService projectCommentService)
        {
            _projectCommentService = projectCommentService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedProjectCommentDto>>> GetProjectCommentsByUserId([FromRoute] int id_user, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'project', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'project_id==1;content_project_comment=like=comment'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'created_at,asc' or 'created_at,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var projectComments = await _projectCommentService.GetProjectCommentsByUserId(id_user, limit, offset, rsqlDto, sortDto, expand);
            return Ok(projectComments);
        }

        [HttpGet("{id_project_comment}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedProjectCommentDto>> GetProjectCommentsById([FromRoute] int id_user, [FromRoute] int id_project_comment,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'project', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projectComment = await _projectCommentService.GetProjectCommentsById(id_project_comment, id_user, null, expand);
            return Ok(projectComment);
        }

        // no create project comment in user controller

        [HttpPut("{id_project_comment}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjectCommentDto>> UpdateProjectComment([FromRoute] int id_user, [FromRoute] int id_project_comment, [FromBody] UpdateProjectCommentDto projectCommentDto)
        {
            var projectComment = await _projectCommentService.UpdateProjectComment(id_project_comment, projectCommentDto, id_user);
            return Ok(projectComment);
        }

        [HttpDelete("{id_project_comment}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteProjectComment([FromRoute] int id_user, [FromRoute] int id_project_comment)
        {
            await _projectCommentService.DeleteProjectComment(id_project_comment, id_user);
            return NoContent();
        }
    }
}