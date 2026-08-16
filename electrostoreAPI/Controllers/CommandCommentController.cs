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
        private readonly ICommandCommentService _commandCommentaireService;

        public CommandCommentController(ICommandCommentService commandCommentaireService)
        {
            _commandCommentaireService = commandCommentaireService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedCommandCommentDto>>> GetCommandsCommentairesByCommandId([FromRoute] int id_command, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'command', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'content_command_comment=like=example'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'created_at,asc' or 'created_at,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var commandCommentaires = await _commandCommentaireService.GetCommandsCommentairesByCommandId(id_command, limit, offset, rsqlDto, sortDto, expand);
            return Ok(commandCommentaires);
        }

        [HttpGet("{id_command_comment}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedCommandCommentDto>> GetCommandsCommentaireById([FromRoute] int id_command, [FromRoute] int id_command_comment,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'command', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var commandCommentaire = await _commandCommentaireService.GetCommandsCommentaireById(id_command_comment, null, id_command, expand);
            return Ok(commandCommentaire);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCommandCommentDto>> CreateCommentaire([FromRoute] int id_command, [FromBody] CreateCommandCommentByCommandDto commandCommentaireDto)
        {
            var commandCommentaireDtoFull = new CreateCommandCommentDto
            {
                id_command = id_command,
                id_user = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : throw new InvalidOperationException("User identifier not found."),
                content_command_comment = commandCommentaireDto.content_command_comment
            };
            var commandCommentaire = await _commandCommentaireService.CreateCommentaire(commandCommentaireDtoFull);
            return CreatedAtAction(nameof(GetCommandsCommentaireById), new { id_command = commandCommentaire.id_command, id_command_comment = commandCommentaire.id_command_comment }, commandCommentaire);
        }

        [HttpPut("{id_command_comment}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCommandCommentDto>> UpdateCommentaire([FromRoute] int id_command, [FromRoute] int id_command_comment, [FromBody] UpdateCommandCommentDto commandCommentaireDto)
        {
            var commandCommentaire = await _commandCommentaireService.UpdateCommentaire(id_command_comment, commandCommentaireDto, null, id_command);
            return Ok(commandCommentaire);
        }

        [HttpDelete("{id_command_comment}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteCommentaire([FromRoute] int id_command, [FromRoute] int id_command_comment)
        {
            await _commandCommentaireService.DeleteCommentaire(id_command_comment, null, id_command);
            return NoContent();
        }
    }
}