using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.CommandItemService;
using Swashbuckle.AspNetCore.Annotations;

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
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadExtendedCommandItemDto>>> GetCommandsItemsByItemId([FromRoute] int id_item, [FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'command', 'item'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var commandItems = await _commandItemService.GetCommandsItemsByItemId(id_item, limit, offset, expand);
            var CountList = await _commandItemService.GetCommandsItemsCountByItemId(id_item);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");
            return Ok(commandItems);
        }

        [HttpGet("{id_command}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedCommandItemDto>> GetCommandItemById([FromRoute] int id_item, [FromRoute] int id_command, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'command', 'item'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var commandItem = await _commandItemService.GetCommandItemById(id_command, id_item, expand);
            return Ok(commandItem);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCommandItemDto>> CreateCommandItem([FromRoute] int id_item, [FromBody] CreateCommandItemByItemDto commandItemDto)
        {
            var commandItemDtoFull = new CreateCommandItemDto
            {
                id_item = id_item,
                id_command = commandItemDto.id_command,
                qte_command_item = commandItemDto.qte_command_item,
                prix_command_item = commandItemDto.prix_command_item
            };
            var commandItem = await _commandItemService.CreateCommandItem(commandItemDtoFull);
            return CreatedAtAction(nameof(GetCommandItemById), new { id_item = commandItem.id_item, id_command = commandItem.id_command }, commandItem);
        }

        [HttpPost("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkCommandItemDto>> CreateBulkCommandItem([FromRoute] int id_item, [FromBody] List<CreateCommandItemByItemDto> commandItemDto)
        {
            var commandItemDtoFull = commandItemDto.Select(x => new CreateCommandItemDto
            {
                id_item = id_item,
                id_command = x.id_command,
                qte_command_item = x.qte_command_item,
                prix_command_item = x.prix_command_item
            }).ToList();
            var commandItems = await _commandItemService.CreateBulkCommandItem(commandItemDtoFull);
            return Ok(commandItems);
        }

        [HttpPut("{id_command}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCommandItemDto>> UpdateCommandItem([FromRoute] int id_item, [FromRoute] int id_command, [FromBody] UpdateCommandItemDto commandItemDto)
        {
            var commandItem = await _commandItemService.UpdateCommandItem(id_command, id_item, commandItemDto);
            return Ok(commandItem);
        }

        [HttpDelete("{id_command}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteCommandItem([FromRoute] int id_item, [FromRoute] int id_command)
        {
            await _commandItemService.DeleteCommandItem(id_command, id_item);
            return NoContent();
        }
    }
}