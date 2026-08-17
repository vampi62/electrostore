using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.CommandCommentService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/user/{id_user}/command_comment")]

    public class UserCommandCommentController : ControllerBase
    {
        private readonly ICommandCommentService _commandCommentService;

        public UserCommandCommentController(ICommandCommentService commandCommentService)
        {
            _commandCommentService = commandCommentService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedCommandCommentDto>>> GetCommandsCommentsByUserId([FromRoute] int id_user, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'command', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'content_command_comment=like=example'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'created_at,asc' or 'created_at,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var commandComments = await _commandCommentService.GetCommandsCommentsByUserId(id_user, limit, offset, rsqlDto, sortDto, expand);
            return Ok(commandComments);
        }

        [HttpGet("{id_command_comment}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedCommandCommentDto>> GetCommandsCommentById([FromRoute] int id_user, [FromRoute] int id_command_comment,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'command', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var commandComment = await _commandCommentService.GetCommandsCommentById(id_command_comment, id_user, null, expand);
            return Ok(commandComment);
        }

        // no create command comment in user controller

        [HttpPut("{id_command_comment}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCommandCommentDto>> UpdateCommandComment([FromRoute] int id_user, [FromRoute] int id_command_comment, [FromBody] UpdateCommandCommentDto commandCommentDto)
        {
            var commandComment = await _commandCommentService.UpdateComment(id_command_comment, commandCommentDto, id_user);
            return Ok(commandComment);
        }

        [HttpDelete("{id_command_comment}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteCommandComment([FromRoute] int id_user, [FromRoute] int id_command_comment)
        {
            await _commandCommentService.DeleteComment(id_command_comment, id_user);
            return NoContent();
        }
    }
}