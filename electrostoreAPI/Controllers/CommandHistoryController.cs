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
        private readonly ICommandHistoryService _historyService;

        public CommandHistoryController(ICommandHistoryService historyService)
        {
            _historyService = historyService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadCommandHistoryDto>>> GetCommandHistoriesByCommandId(
            [FromRoute] int id_command, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
            [FromQuery, SwaggerParameter(Description = "(Optional) RSQL filter. Example: 'status_projet==0'.")] string? filter = null,
            [FromQuery, SwaggerParameter(Description = "(Optional) Sort string. Example: 'created_at,asc' or 'created_at,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var history = await _historyService.GetCommandHistoriesByCommandId(id_command, limit, offset, rsqlDto, sortDto);
            return Ok(history);
        }

        [HttpGet("{id_command_history}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCommandHistoryDto>> GetCommandHistoryById(
            [FromRoute] int id_command, [FromRoute] int id_command_history)
        {
            var history = await _historyService.GetCommandHistoryById(id_command_history, id_command);
            return Ok(history);
        }
    }
}
