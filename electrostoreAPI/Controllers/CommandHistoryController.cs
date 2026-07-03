using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.CommandHistoryService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/command/{id_command}/history")]

    public class CommandHistoryController : ControllerBase
    {
        private readonly ICommandHistoryService _commandHistoryService;

        public CommandHistoryController(ICommandHistoryService commandHistoryService)
        {
            _commandHistoryService = commandHistoryService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadCommandHistoryDto>>> GetCommandHistoryByCommandId([FromRoute] int id_command, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'prix_command_item=gt=100'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'prix_command_item,asc' or 'prix_command_item,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var commandHistorys = await _commandHistoryService.GetCommandHistoryByCommandId(id_command, limit, offset, rsqlDto, sortDto);
            return Ok(commandHistorys);
        }

        [HttpGet("{id_history}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCommandHistoryDto>> GetCommandHistoryById([FromRoute] int id_command, [FromRoute] int id_history)
        {
            var commandHistory = await _commandHistoryService.GetCommandHistoryById(id_history, id_command);
            return Ok(commandHistory);
        }
    }
}