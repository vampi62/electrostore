using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.CommandItemService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/item/{id_item}/command")]

    public class ItemCommandController : ControllerBase
    {
        private readonly ICommandItemService _commandItemService;

        public ItemCommandController(ICommandItemService commandItemService)
        {
            _commandItemService = commandItemService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReadCommandItemDto>>> GetCommandItemsByItemId([FromRoute] int id_item, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var commandItems = await _commandItemService.GetCommandItemsByItemId(id_item, limit, offset);
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

        [HttpGet("{id_command}")]
        public async Task<ActionResult<ReadCommandItemDto>> GetCommandItemById([FromRoute] int id_item, [FromRoute] int id_command)
        {
            var commandItem = await _commandItemService.GetCommandItemById(id_item, id_command);
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
        public async Task<ActionResult<ReadCommandItemDto>> CreateCommandItem([FromRoute] int id_item, [FromBody] CreateCommandItemByItemDto commandItemDto)
        {
            var commandItemDtoFull = new CreateCommandItemDto
            {
                id_item = id_item,
                id_command = commandItemDto.id_command,
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
            return CreatedAtAction(nameof(GetCommandItemById), new { id_item = commandItem.Value.id_item, id_command = commandItem.Value.id_command }, commandItem.Value);
        }

        [HttpPut("{id_command}")]
        public async Task<ActionResult<ReadCommandItemDto>> UpdateCommandItem([FromRoute] int id_item, [FromRoute] int id_command, [FromBody] UpdateCommandItemDto commandItemDto)
        {
            var commandItem = await _commandItemService.UpdateCommandItem(id_item, id_command, commandItemDto);
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

        [HttpDelete("{id_command}")]
        public async Task<ActionResult> DeleteCommandItem([FromRoute] int id_item, [FromRoute] int id_command)
        {
            await _commandItemService.DeleteCommandItem(id_item, id_command);
            return NoContent();
        }
    }
}