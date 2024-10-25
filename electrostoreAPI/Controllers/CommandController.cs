using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.CommandService;

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
        [Authorize(Policy = "AccessTokenPolicy")]
        public async Task<ActionResult<IEnumerable<ReadCommandDto>>> GetCommands([FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var commands = await _commandService.GetCommands(limit, offset);
            return Ok(commands);
        }

        [HttpGet("{id_command}")]
        [Authorize(Policy = "AccessTokenPolicy")]
        public async Task<ActionResult<ReadCommandDto>> GetCommandById([FromRoute] int id_command)
        {
            var command = await _commandService.GetCommandById(id_command);
            return Ok(command);
        }

        [HttpPost]
        [Authorize(Policy = "AccessTokenPolicy")]
        public async Task<ActionResult<ReadCommandDto>> CreateCommand([FromBody] CreateCommandDto commandDto)
        {
            var command = await _commandService.CreateCommand(commandDto);
            return CreatedAtAction(nameof(GetCommandById), new { id_command = command.id_command }, command);
        }

        [HttpPut("{id_command}")]
        [Authorize(Policy = "AccessTokenPolicy")]
        public async Task<ActionResult<ReadCommandDto>> UpdateCommand([FromRoute] int id_command, [FromBody] UpdateCommandDto commandDto)
        {
            var command = await _commandService.UpdateCommand(id_command, commandDto);
            return Ok(command);
        }

        [HttpDelete("{id_command}")]
        [Authorize(Policy = "AccessTokenPolicy")]
        public async Task<ActionResult> DeleteCommand([FromRoute] int id_command)
        {
            await _commandService.DeleteCommand(id_command);
            return NoContent();
        }
    }
}