using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.EquipementBoxService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/store/{id_store}/box/{id_box}/equipement")]

    public class StoreBoxEquipementController : ControllerBase
    {
        private readonly IEquipementBoxService _equipementBoxService;

        public StoreBoxEquipementController(IEquipementBoxService equipementBoxService)
        {
            _equipementBoxService = equipementBoxService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedEquipementBoxDto>>> GetEquipementsBoxsByBoxId([FromRoute] int id_store, [FromRoute] int id_box, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'equipement', 'box'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'id_equipement==1'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'id_equipement,asc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            await _equipementBoxService.CheckIfStoreExists(id_store, id_box);
            var equipementsBoxs = await _equipementBoxService.GetEquipementsBoxsByBoxId(id_box, limit, offset, rsqlDto, sortDto, expand);
            return Ok(equipementsBoxs);
        }

        [HttpGet("{id_equipement}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedEquipementBoxDto>> GetEquipementBoxById([FromRoute] int id_store, [FromRoute] int id_box, [FromRoute] int id_equipement,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'equipement', 'box'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            await _equipementBoxService.CheckIfStoreExists(id_store, id_box);
            var equipementBox = await _equipementBoxService.GetEquipementBoxById(id_equipement, id_box, expand);
            return Ok(equipementBox);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadEquipementBoxDto>> CreateEquipementBox([FromRoute] int id_store, [FromRoute] int id_box, [FromBody] CreateEquipementBoxByBoxDto equipementBoxDto)
        {
            await _equipementBoxService.CheckIfStoreExists(id_store, id_box);
            var equipementBoxDtoFull = new CreateEquipementBoxDto
            {
                id_box = id_box,
                id_equipement = equipementBoxDto.id_equipement
            };
            var equipementBox = await _equipementBoxService.CreateEquipementBox(equipementBoxDtoFull);
            return CreatedAtAction(nameof(GetEquipementBoxById), new { id_store = id_store, id_box = equipementBox.id_box, id_equipement = equipementBox.id_equipement }, equipementBox);
        }

        [HttpDelete("{id_equipement}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteEquipementBox([FromRoute] int id_store, [FromRoute] int id_box, [FromRoute] int id_equipement)
        {
            await _equipementBoxService.CheckIfStoreExists(id_store, id_box);
            await _equipementBoxService.DeleteEquipementBox(id_equipement, id_box);
            return NoContent();
        }
    }
}
