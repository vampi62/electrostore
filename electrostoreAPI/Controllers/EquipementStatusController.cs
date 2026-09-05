using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.EquipementStatusService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/equipement/{id_equipement}/status-history")]

    public class EquipementStatusController : ControllerBase
    {
        private readonly IEquipementStatusService _equipementStatusService;

        public EquipementStatusController(IEquipementStatusService equipementStatusService)
        {
            _equipementStatusService = equipementStatusService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedEquipementStatusDto>>> GetEquipementStatusByEquipementId([FromRoute] int id_equipement, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL filter. Example: 'status_equipement==0'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string. Example: 'created_at,asc' or 'created_at,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var equipementStatus = await _equipementStatusService.GetEquipementStatusByEquipementId(id_equipement, limit, offset, rsqlDto, sortDto);
            return Ok(equipementStatus);
        }

        [HttpGet("{id_equipement_status}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedEquipementStatusDto>> GetEquipementStatusById([FromRoute] int id_equipement, [FromRoute] int id_equipement_status)
        {
            var equipementStatus = await _equipementStatusService.GetEquipementStatusById(id_equipement_status, id_equipement);
            return Ok(equipementStatus);
        }
    }
}
