using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.EquipementService;
using ElectrostoreAPI.Services.FileService;
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
        private readonly IFileService _fileService;

        public EquipementController(IEquipementService equipementService, IFileService fileService)
        {
            _equipementService = equipementService;
            _fileService = fileService;
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
        public async Task<ActionResult<ReadEquipementDto>> CreateEquipement([FromForm] CreateEquipementDto equipementDto)
        {
            var equipement = await _equipementService.CreateEquipement(equipementDto);
            return CreatedAtAction(nameof(GetEquipementById), new { id_equipement = equipement.id_equipement }, equipement);
        }

        [HttpPut("{id_equipement}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadEquipementDto>> UpdateEquipement([FromRoute] int id_equipement, [FromForm] UpdateEquipementDto equipementDto)
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

        [HttpGet("{id_equipement}/picture")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> GetEquipementPicture([FromRoute] int id_equipement)
        {
            var equipement = await _equipementService.GetEquipementById(id_equipement);
            if (string.IsNullOrEmpty(equipement.url_picture_equipement))
            {
                return NotFound();
            }
            var result = await _fileService.GetFile(equipement.url_picture_equipement);
            if (result.success && result.file_stream != null)
            {
                return File(result.file_stream, result.mime_type);
            }
            return NotFound(result.error_message);
        }

        [HttpGet("{id_equipement}/thumbnail")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> GetEquipementThumbnail([FromRoute] int id_equipement)
        {
            var equipement = await _equipementService.GetEquipementById(id_equipement);
            if (string.IsNullOrEmpty(equipement.url_thumbnail_equipement))
            {
                return NotFound();
            }
            var result = await _fileService.GetFile(equipement.url_thumbnail_equipement);
            if (result.success && result.file_stream != null)
            {
                return File(result.file_stream, result.mime_type);
            }
            return NotFound(result.error_message);
        }
    }
}
