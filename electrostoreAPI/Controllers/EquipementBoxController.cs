using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.EquipementBoxService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/equipement/{id_equipement}/box")]

    public class EquipementBoxController : ControllerBase
    {
        private readonly IEquipementBoxService _equipementBoxService;

        public EquipementBoxController(IEquipementBoxService equipementBoxService)
        {
            _equipementBoxService = equipementBoxService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedEquipementBoxDto>>> GetEquipementsBoxsByEquipementId([FromRoute] int id_equipement, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'equipement', 'box'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'id_box==1'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'id_box,asc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var equipementBoxs = await _equipementBoxService.GetEquipementsBoxsByEquipementId(id_equipement, limit, offset, rsqlDto, sortDto, expand);
            return Ok(equipementBoxs);
        }

        [HttpGet("{id_box}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedEquipementBoxDto>> GetEquipementBoxById([FromRoute] int id_equipement, [FromRoute] int id_box,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'equipement', 'box'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var equipementBox = await _equipementBoxService.GetEquipementBoxById(id_equipement, id_box, expand);
            return Ok(equipementBox);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadEquipementBoxDto>> CreateEquipementBox([FromRoute] int id_equipement, [FromBody] CreateEquipementBoxByEquipementDto equipementBoxDto)
        {
            var equipementBoxDtoFull = new CreateEquipementBoxDto
            {
                id_equipement = id_equipement,
                id_box = equipementBoxDto.id_box
            };
            var equipementBox = await _equipementBoxService.CreateEquipementBox(equipementBoxDtoFull);
            return CreatedAtAction(nameof(GetEquipementBoxById), new { id_equipement = equipementBox.id_equipement, id_box = equipementBox.id_box }, equipementBox);
        }

        [HttpDelete("{id_box}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteEquipementBox([FromRoute] int id_equipement, [FromRoute] int id_box)
        {
            await _equipementBoxService.DeleteEquipementBox(id_equipement, id_box);
            return NoContent();
        }
    }
}
