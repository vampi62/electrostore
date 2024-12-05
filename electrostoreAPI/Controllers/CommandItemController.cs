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
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadCommandItemDto>>> GetCommandItemsByCommandId([FromRoute] int id_command, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var commandItems = await _commandItemService.GetCommandItemsByCommandId(id_command, limit, offset);
            var CountList = await _commandItemService.GetCommandItemsCountByCommandId(id_command);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");
            return Ok(commandItems);
        }

        [HttpGet("{id_item}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCommandItemDto>> GetCommandItemById([FromRoute] int id_command, [FromRoute] int id_item)
        {
            var commandItem = await _commandItemService.GetCommandItemById(id_command, id_item);
            return Ok(commandItem);
        }

        [HttpPost("{id_item}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCommandItemDto>> CreateCommandItem([FromRoute] int id_command, [FromRoute] int id_item, [FromBody] CreateCommandItemByCommandDto commandItemDto)
        {
            var commandItemDtoFull = new CreateCommandItemDto
            {
                id_item = id_item,
                id_command = id_command,
                qte_command_item = commandItemDto.qte_command_item,
                prix_command_item = commandItemDto.prix_command_item
            };
            var commandItem = await _commandItemService.CreateCommandItem(commandItemDtoFull);
            return CreatedAtAction(nameof(GetCommandItemById), new { id_command = commandItem.id_command, id_item = commandItem.id_item }, commandItem);
        }

        [HttpPut("{id_item}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCommandItemDto>> UpdateCommandItem([FromRoute] int id_command, [FromRoute] int id_item, [FromBody] UpdateCommandItemDto commandItemDto)
        {
            var commandItem = await _commandItemService.UpdateCommandItem(id_command, id_item, commandItemDto);
            return Ok(commandItem);
        }

        [HttpDelete("{id_item}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteCommandItem([FromRoute] int id_command, [FromRoute] int id_item)
        {
            await _commandItemService.DeleteCommandItem(id_command, id_item);
            return NoContent();
        }
    }
}