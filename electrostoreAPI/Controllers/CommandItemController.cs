using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.CommandItemService;
using Swashbuckle.AspNetCore.Annotations;

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
        public async Task<ActionResult<IEnumerable<ReadExtendedCommandItemDto>>> GetCommandsItemsByCommandId([FromRoute] int id_command, [FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'command', 'item'. Multiple values can be specified by separating them with ','.")] string? expand = null)
        {
            var commandItems = await _commandItemService.GetCommandsItemsByCommandId(id_command, limit, offset, expand?.Split(',').ToList());
            var CountList = await _commandItemService.GetCommandsItemsCountByCommandId(id_command);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");
            return Ok(commandItems);
        }

        [HttpGet("{id_item}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedCommandItemDto>> GetCommandItemById([FromRoute] int id_command, [FromRoute] int id_item, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'command', 'item'. Multiple values can be specified by separating them with ','.")] string? expand = null)
        {
            var commandItem = await _commandItemService.GetCommandItemById(id_command, id_item, expand?.Split(',').ToList());
            return Ok(commandItem);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCommandItemDto>> CreateCommandItem([FromRoute] int id_command, [FromBody] CreateCommandItemByCommandDto commandItemDto)
        {
            var commandItemDtoFull = new CreateCommandItemDto
            {
                id_item = commandItemDto.id_item,
                id_command = id_command,
                qte_command_item = commandItemDto.qte_command_item,
                prix_command_item = commandItemDto.prix_command_item
            };
            var commandItem = await _commandItemService.CreateCommandItem(commandItemDtoFull);
            return CreatedAtAction(nameof(GetCommandItemById), new { id_command = commandItem.id_command, id_item = commandItem.id_item }, commandItem);
        }

        [HttpPost("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkCommandItemDto>> CreateBulkCommandItem([FromRoute] int id_command, [FromBody] List<CreateCommandItemByCommandDto> commandItemDto)
        {
            var commandItemDtoFull = commandItemDto.Select(x => new CreateCommandItemDto
            {
                id_item = x.id_item,
                id_command = id_command,
                qte_command_item = x.qte_command_item,
                prix_command_item = x.prix_command_item
            }).ToList();
            var commandItems = await _commandItemService.CreateBulkCommandItem(commandItemDtoFull);
            return Ok(commandItems);
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