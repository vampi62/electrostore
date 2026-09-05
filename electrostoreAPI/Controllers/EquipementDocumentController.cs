using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.EquipementDocumentService;
using ElectrostoreAPI.Services.FileService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/equipement/{id_equipement}/document")]

    public class EquipementDocumentController : ControllerBase
    {
        private readonly IEquipementDocumentService _equipementDocumentService;
        private readonly IFileService _fileService;

        public EquipementDocumentController(IEquipementDocumentService equipementDocumentService, IFileService fileService)
        {
            _equipementDocumentService = equipementDocumentService;
            _fileService = fileService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadEquipementDocumentDto>>> GetEquipementsDocumentsByEquipementId([FromRoute] int id_equipement, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'name_equipement_document=like=example'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'name_equipement_document,asc' or 'name_equipement_document,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var equipementsDocuments = await _equipementDocumentService.GetEquipementsDocumentsByEquipementId(id_equipement, limit, offset, rsqlDto, sortDto);
            return Ok(equipementsDocuments);
        }

        [HttpGet("{id_equipementDocument}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadEquipementDocumentDto>> GetEquipementDocumentById([FromRoute] int id_equipementDocument, [FromRoute] int id_equipement)
        {
            var equipementDocument = await _equipementDocumentService.GetEquipementDocumentById(id_equipementDocument, id_equipement);
            return Ok(equipementDocument);
        }

        [HttpGet("{id_equipementDocument}/download")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DownloadEquipementDocument([FromRoute] int id_equipementDocument, [FromRoute] int id_equipement)
        {
            var equipementDocument = await _equipementDocumentService.GetEquipementDocumentById(id_equipementDocument, id_equipement);
            var result = await _fileService.GetFile(equipementDocument.url_equipement_document);
            if (result.success && result.file_stream != null)
            {
                return File(result.file_stream, result.mime_type, equipementDocument.name_equipement_document);
            }
            else
            {
                return NotFound(result.error_message);
            }
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadEquipementDocumentDto>> CreateEquipementDocument([FromForm] CreateEquipementDocumentByEquipementDto equipementDocumentDto, [FromRoute] int id_equipement)
        {
            var equipementDocumentDtoFull = new CreateEquipementDocumentDto
            {
                id_equipement = id_equipement,
                name_equipement_document = equipementDocumentDto.name_equipement_document,
                document = equipementDocumentDto.document
            };
            var equipementDocument = await _equipementDocumentService.CreateEquipementDocument(equipementDocumentDtoFull);
            return CreatedAtAction(nameof(GetEquipementDocumentById), new { id_equipementDocument = equipementDocument.id_equipement_document, id_equipement = equipementDocument.id_equipement }, equipementDocument);
        }

        [HttpPut("{id_equipementDocument}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadEquipementDocumentDto>> UpdateEquipementDocument([FromRoute] int id_equipementDocument, [FromBody] UpdateEquipementDocumentDto equipementDocumentDto, [FromRoute] int id_equipement)
        {
            var equipementDocument = await _equipementDocumentService.UpdateEquipementDocument(id_equipementDocument, equipementDocumentDto, id_equipement);
            return Ok(equipementDocument);
        }

        [HttpDelete("{id_equipementDocument}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteEquipementDocument([FromRoute] int id_equipementDocument, [FromRoute] int id_equipement)
        {
            await _equipementDocumentService.DeleteEquipementDocument(id_equipementDocument, id_equipement);
            return NoContent();
        }
    }
}
