using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.CommandItemService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/command/{id_command}/item")]

    public class CommandItemController : ControllerBase
    {
        private readonly ICommandItemService _commandItemService;

        public CommandItemController(ICommandItemService commandItemService)
        {
            _commandItemService = commandItemService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReadCommandItemDto>>> GetCommandItemsByCommandId([FromRoute] int id_command, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var commandItems = await _commandItemService.GetCommandItemsByCommandId(id_command, limit, offset);
            if (commandItems.Result is BadRequestObjectResult)
            {
                return commandItems.Result;
            }
            if (commandItems.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(commandItems.Value);
        }

        [HttpGet("{id_item}")]
        public async Task<ActionResult<ReadCommandItemDto>> GetCommandItemById([FromRoute] int id_command, [FromRoute] int id_item)
        {
            var commandItem = await _commandItemService.GetCommandItemById(id_command, id_item);
            if (commandItem.Result is BadRequestObjectResult)
            {
                return commandItem.Result;
            }
            if (commandItem.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(commandItem.Value);
        }

        [HttpPost]
        public async Task<ActionResult<ReadCommandItemDto>> CreateCommandItem([FromRoute] int id_command, [FromBody] CreateCommandItemByCommandDto commandItemDto)
        {
            var commandItemDtoFull = new CreateCommandItemDto
            {
                id_item = commandItemDto.id_item,
                id_command = id_command,
                qte_commanditem = commandItemDto.qte_commanditem,
                prix_commanditem = commandItemDto.prix_commanditem
            };
            var commandItem = await _commandItemService.CreateCommandItem(commandItemDtoFull);
            if (commandItem.Result is BadRequestObjectResult)
            {
                return commandItem.Result;
            }
            if (commandItem.Value == null)
            {
                return StatusCode(500);
            }
            return CreatedAtAction(nameof(GetCommandItemById), new { id_command = commandItem.Value.id_command, id_item = commandItem.Value.id_item }, commandItem.Value);
        }

        [HttpPut("{id_item}")]
        public async Task<ActionResult<ReadCommandItemDto>> UpdateCommandItem([FromRoute] int id_command, [FromRoute] int id_item, [FromBody] UpdateCommandItemDto commandItemDto)
        {
            var commandItem = await _commandItemService.UpdateCommandItem(id_command, id_item, commandItemDto);
            if (commandItem.Result is BadRequestObjectResult)
            {
                return commandItem.Result;
            }
            if (commandItem.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(commandItem.Value);
        }

        [HttpDelete("{id_item}")]
        public async Task<ActionResult> DeleteCommandItem([FromRoute] int id_command, [FromRoute] int id_item)
        {
            await _commandItemService.DeleteCommandItem(id_command, id_item);
            return NoContent();
        }
    }
}