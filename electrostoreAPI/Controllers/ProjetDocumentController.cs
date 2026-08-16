using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Services.ProjetDocumentService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/projet/{id_project}/document")]

    public class ProjetDocumentController : ControllerBase
    {
        private readonly IProjetDocumentService _projetDocumentService;
        private readonly IFileService _fileService;

        public ProjetDocumentController(IProjetDocumentService projetDocumentService, IFileService fileService)
        {
            _projetDocumentService = projetDocumentService;
            _fileService = fileService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadProjetDocumentDto>>> GetProjetsDocumentsByProjetId([FromRoute] int id_project, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'name_project_document=like=example'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'name_project_document,asc' or 'name_project_document,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var projetsDocuments = await _projetDocumentService.GetProjetDocumentsByProjetId(id_project, limit, offset, rsqlDto, sortDto);
            return Ok(projetsDocuments);
        }

        [HttpGet("{id_projetDocument}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetDocumentDto>> GetProjetDocumentById([FromRoute] int id_projetDocument, [FromRoute] int id_project)
        {
            var projetDocument = await _projetDocumentService.GetProjetDocumentById(id_projetDocument, id_project);
            return Ok(projetDocument);
        }
        
        [HttpGet("{id_projetDocument}/download")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DownloadProjetDocument([FromRoute] int id_projetDocument, [FromRoute] int id_project)
        {
            var projetDocument = await _projetDocumentService.GetProjetDocumentById(id_projetDocument, id_project);
            var result = await _fileService.GetFile(projetDocument.url_project_document);
            if (result.Success && result.FileStream != null)
            {
                return File(result.FileStream, result.MimeType, projetDocument.name_project_document);
            }
            else
            {
                return NotFound(result.ErrorMessage);
            }
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetDocumentDto>> CreateProjetDocument([FromForm] CreateProjetDocumentByProjetDto projetDocumentDto, [FromRoute] int id_project)
        {
            var projetDocumentDtoFull = new CreateProjetDocumentDto
            {
                id_project = id_project,
                name_project_document = projetDocumentDto.name_project_document,
                document = projetDocumentDto.document
            };
            var projetDocument = await _projetDocumentService.CreateProjetDocument(projetDocumentDtoFull);
            return CreatedAtAction(nameof(GetProjetDocumentById), new { id_projetDocument = projetDocument.id_project_document, id_project = projetDocument.id_project }, projetDocument);
        }

        [HttpPut("{id_projetDocument}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetDocumentDto>> UpdateProjetDocument([FromRoute] int id_projetDocument, [FromBody] UpdateProjetDocumentDto projetDocumentDto, [FromRoute] int id_project)
        {
            var projetDocument = await _projetDocumentService.UpdateProjetDocument(id_projetDocument, projetDocumentDto, id_project);
            return Ok(projetDocument);
        }

        [HttpDelete("{id_projetDocument}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteProjetDocument([FromRoute] int id_projetDocument, [FromRoute] int id_project)
        {
            await _projetDocumentService.DeleteProjetDocument(id_projetDocument, id_project);
            return NoContent();
        }
    }
}