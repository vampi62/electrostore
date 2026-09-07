using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.EquipementMaintenanceService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/equipement/{id_equipement}/maintenance")]

    public class EquipementMaintenanceController : ControllerBase
    {
        private readonly IEquipementMaintenanceService _equipementMaintenanceService;

        public EquipementMaintenanceController(IEquipementMaintenanceService equipementMaintenanceService)
        {
            _equipementMaintenanceService = equipementMaintenanceService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedEquipementMaintenanceDto>>> GetEquipementsMaintenancesByEquipementId([FromRoute] int id_equipement, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'equipement', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'type_equipement_maintenance==0'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'date_planned_equipement_maintenance,asc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var equipementMaintenances = await _equipementMaintenanceService.GetEquipementsMaintenancesByEquipementId(id_equipement, limit, offset, rsqlDto, sortDto, expand);
            return Ok(equipementMaintenances);
        }

        [HttpGet("{id_equipement_maintenance}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedEquipementMaintenanceDto>> GetEquipementMaintenanceById([FromRoute] int id_equipement, [FromRoute] int id_equipement_maintenance,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'equipement', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var equipementMaintenance = await _equipementMaintenanceService.GetEquipementMaintenanceById(id_equipement_maintenance, id_equipement, expand);
            return Ok(equipementMaintenance);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadEquipementMaintenanceDto>> CreateEquipementMaintenance([FromRoute] int id_equipement, [FromBody] CreateEquipementMaintenanceByEquipementDto equipementMaintenanceDto)
        {
            var equipementMaintenanceDtoFull = new CreateEquipementMaintenanceDto
            {
                id_equipement = id_equipement,
                id_user = equipementMaintenanceDto.id_user,
                type_equipement_maintenance = equipementMaintenanceDto.type_equipement_maintenance,
                date_planned_equipement_maintenance = equipementMaintenanceDto.date_planned_equipement_maintenance,
                description_equipement_maintenance = equipementMaintenanceDto.description_equipement_maintenance
            };
            var equipementMaintenance = await _equipementMaintenanceService.CreateEquipementMaintenance(equipementMaintenanceDtoFull);
            return CreatedAtAction(nameof(GetEquipementMaintenanceById), new { id_equipement = equipementMaintenance.id_equipement, id_equipement_maintenance = equipementMaintenance.id_equipement_maintenance }, equipementMaintenance);
        }

        [HttpPut("{id_equipement_maintenance}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadEquipementMaintenanceDto>> UpdateEquipementMaintenance([FromRoute] int id_equipement, [FromRoute] int id_equipement_maintenance, [FromBody] UpdateEquipementMaintenanceDto equipementMaintenanceDto)
        {
            var equipementMaintenance = await _equipementMaintenanceService.UpdateEquipementMaintenance(id_equipement_maintenance, equipementMaintenanceDto, id_equipement);
            return Ok(equipementMaintenance);
        }

        [HttpDelete("{id_equipement_maintenance}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteEquipementMaintenance([FromRoute] int id_equipement, [FromRoute] int id_equipement_maintenance)
        {
            await _equipementMaintenanceService.DeleteEquipementMaintenance(id_equipement_maintenance, id_equipement);
            return NoContent();
        }
    }
}
