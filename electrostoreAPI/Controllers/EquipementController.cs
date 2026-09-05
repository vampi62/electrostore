using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.EquipementService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/equipement")]

    public class EquipementController : ControllerBase
    {
        private readonly IEquipementService _equipementService;

        public EquipementController(IEquipementService equipementService)
        {
            _equipementService = equipementService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedEquipementDto>>> GetEquipements([FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'equipement_tags', 'equipement_boxs', 'equipement_documents', 'equipement_maintenances', 'equipement_status_history', 'equipement_comments'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to select list of ID to research in the base. Multiple values can be specified by separating them with ','.")] List<int>? idResearch = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'reference_name_equipement=like=example'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'reference_name_equipement,asc' or 'reference_name_equipement,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var equipements = await _equipementService.GetEquipements(limit, offset, rsqlDto, sortDto, expand, idResearch);
            return Ok(equipements);
        }

        [HttpGet("{id_equipement}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedEquipementDto>> GetEquipementById([FromRoute] int id_equipement,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'equipement_tags', 'equipement_boxs', 'equipement_documents', 'equipement_maintenances', 'equipement_status_history', 'equipement_comments'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var equipement = await _equipementService.GetEquipementById(id_equipement, expand);
            return Ok(equipement);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadEquipementDto>> CreateEquipement([FromBody] CreateEquipementDto equipementDto)
        {
            var equipement = await _equipementService.CreateEquipement(equipementDto);
            return CreatedAtAction(nameof(GetEquipementById), new { id_equipement = equipement.id_equipement }, equipement);
        }

        [HttpPut("{id_equipement}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadEquipementDto>> UpdateEquipement([FromRoute] int id_equipement, [FromBody] UpdateEquipementDto equipementDto)
        {
            var equipement = await _equipementService.UpdateEquipement(id_equipement, equipementDto);
            return Ok(equipement);
        }

        [HttpDelete("{id_equipement}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteEquipement([FromRoute] int id_equipement)
        {
            await _equipementService.DeleteEquipement(id_equipement);
            return NoContent();
        }
    }
}
