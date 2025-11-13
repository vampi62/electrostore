using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.CommandService;
using Swashbuckle.AspNetCore.Annotations;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/command")]

    public class CommandController : ControllerBase
    {
        private readonly ICommandService _commandService;

        public CommandController(ICommandService commandService)
        {
            _commandService = commandService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadExtendedCommandDto>>> GetCommands([FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'commands_commentaires', 'commands_documents', 'commands_items'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to select list of ID to research in the base. Multiple values can be specified by separating them with ','.")] List<int>? idResearch = null)
        {
            var commands = await _commandService.GetCommands(limit, offset, expand, idResearch);
            var CountList = await _commandService.GetCommandsCount();
            Response.Headers["X-Total-Count"] = CountList.ToString();
            Response.Headers.AccessControlExposeHeaders = "X-Total-Count";
            return Ok(commands);
        }

        [HttpGet("{id_command}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedCommandDto>> GetCommandById([FromRoute] int id_command, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'commands_commentaires', 'commands_documents', 'commands_items'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var command = await _commandService.GetCommandById(id_command, expand);
            return Ok(command);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCommandDto>> CreateCommand([FromBody] CreateCommandDto commandDto)
        {
            var command = await _commandService.CreateCommand(commandDto);
            return CreatedAtAction(nameof(GetCommandById), new { id_command = command.id_command }, command);
        }

        [HttpPut("{id_command}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCommandDto>> UpdateCommand([FromRoute] int id_command, [FromBody] UpdateCommandDto commandDto)
        {
            var command = await _commandService.UpdateCommand(id_command, commandDto);
            return Ok(command);
        }

        [HttpDelete("{id_command}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteCommand([FromRoute] int id_command)
        {
            await _commandService.DeleteCommand(id_command);
            return NoContent();
        }
    }
}