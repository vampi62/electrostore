using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ProjetDocumentService;
using electrostore.Services.FileService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/projet/{id_projet}/document")]

    public class ProjetDocumentController : ControllerBase
    {
        private readonly IProjetDocumentService _projetDocumentService;
        private readonly FileService _fileService;
        private readonly string _projetDocumentsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/projetDocuments");

        public ProjetDocumentController(IProjetDocumentService projetDocumentService, FileService fileService)
        {
            _projetDocumentService = projetDocumentService;
            _fileService = fileService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadProjetDocumentDto>>> GetProjetsDocumentsByProjetId([FromRoute] int id_projet, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var projetsDocuments = await _projetDocumentService.GetProjetDocumentsByProjetId(id_projet, limit, offset);
            var CountList = await _projetDocumentService.GetProjetDocumentsCountByProjetId(id_projet);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");
            return Ok(projetsDocuments);
        }

        [HttpGet("{id_projetDocument}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetDocumentDto>> GetProjetDocumentById([FromRoute] int id_projetDocument, [FromRoute] int id_projet)
        {
            var projetDocument = await _projetDocumentService.GetProjetDocumentById(id_projetDocument, id_projet);
            return Ok(projetDocument);
        }
        
        [HttpGet("{id_projetDocument}/download")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DownloadProjetDocument([FromRoute] int id_projetDocument, [FromRoute] int id_projet)
        {
            var projetDocument = await _projetDocumentService.GetProjetDocumentById(id_projetDocument, id_projet);
            var result = await _fileService.GetFile(_projetDocumentsPath, projetDocument.url_projet_document); // check if projetDocument.url_projet_document is a valid path
            if (result.Success)
            {
                return PhysicalFile(result.FilePath, result.MimeType, projetDocument.name_projet_document);
            }
            else
            {
                return NotFound(result.ErrorMessage);
            }
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetDocumentDto>> CreateProjetDocument([FromForm] CreateProjetDocumentByProjetDto projetDocumentDto, [FromRoute] int id_projet)
        {
            var projetDocumentDtoFull = new CreateProjetDocumentDto
            {
                id_projet = id_projet,
                name_projet_document = projetDocumentDto.name_projet_document,
                document = projetDocumentDto.document
            };
            var projetDocument = await _projetDocumentService.CreateProjetDocument(projetDocumentDtoFull);
            return CreatedAtAction(nameof(GetProjetDocumentById), new { id_projetDocument = projetDocument.id_projet_document, id_projet = projetDocument.id_projet }, projetDocument);
        }

        [HttpPut("{id_projetDocument}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetDocumentDto>> UpdateProjetDocument([FromRoute] int id_projetDocument, [FromBody] UpdateProjetDocumentDto projetDocumentDto, [FromRoute] int id_projet)
        {
            var projetDocument = await _projetDocumentService.UpdateProjetDocument(id_projetDocument, projetDocumentDto, id_projet);
            return Ok(projetDocument);
        }

        [HttpDelete("{id_projetDocument}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteProjetDocument([FromRoute] int id_projetDocument, [FromRoute] int id_projet)
        {
            await _projetDocumentService.DeleteProjetDocument(id_projetDocument, id_projet);
            return NoContent();
        }
    }
}