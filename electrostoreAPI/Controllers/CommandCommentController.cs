using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.CommandCommentService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/command/{id_command}/comment")]

    public class CommandCommentController : ControllerBase
    {
        private readonly ICommandCommentService _commandCommentService;

        public CommandCommentController(ICommandCommentService commandCommentService)
        {
            _commandCommentService = commandCommentService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedCommandCommentDto>>> GetCommandsCommentsByCommandId([FromRoute] int id_command, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'command', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'content_command_comment=like=example'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'created_at,asc' or 'created_at,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var commandComments = await _commandCommentService.GetCommandsCommentsByCommandId(id_command, limit, offset, rsqlDto, sortDto, expand);
            return Ok(commandComments);
        }

        [HttpGet("{id_command_comment}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedCommandCommentDto>> GetCommandsCommentById([FromRoute] int id_command, [FromRoute] int id_command_comment,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'command', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var commandComment = await _commandCommentService.GetCommandsCommentById(id_command_comment, null, id_command, expand);
            return Ok(commandComment);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCommandCommentDto>> CreateComment([FromRoute] int id_command, [FromBody] CreateCommandCommentByCommandDto commandCommentDto)
        {
            var commandCommentDtoFull = new CreateCommandCommentDto
            {
                id_command = id_command,
                id_user = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : throw new InvalidOperationException("User identifier not found."),
                content_command_comment = commandCommentDto.content_command_comment
            };
            var commandComment = await _commandCommentService.CreateComment(commandCommentDtoFull);
            return CreatedAtAction(nameof(GetCommandsCommentById), new { id_command = commandComment.id_command, id_command_comment = commandComment.id_command_comment }, commandComment);
        }

        [HttpPut("{id_command_comment}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCommandCommentDto>> UpdateComment([FromRoute] int id_command, [FromRoute] int id_command_comment, [FromBody] UpdateCommandCommentDto commandCommentDto)
        {
            var commandComment = await _commandCommentService.UpdateComment(id_command_comment, commandCommentDto, null, id_command);
            return Ok(commandComment);
        }

        [HttpDelete("{id_command_comment}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteComment([FromRoute] int id_command, [FromRoute] int id_command_comment)
        {
            await _commandCommentService.DeleteComment(id_command_comment, null, id_command);
            return NoContent();
        }
    }
}