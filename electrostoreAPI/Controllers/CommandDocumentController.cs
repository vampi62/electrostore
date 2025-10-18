using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.CommandDocumentService;
using electrostore.Services.FileService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/command/{id_command}/document")]

    public class CommandDocumentController : ControllerBase
    {
        private readonly ICommandDocumentService _commandDocumentService;
        private readonly IFileService _fileService;

        public CommandDocumentController(ICommandDocumentService commandDocumentService, IFileService fileService)
        {
            _commandDocumentService = commandDocumentService;
            _fileService = fileService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadCommandDocumentDto>>> GetCommandsDocumentsByCommandId([FromRoute] int id_command, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var commandsDocuments = await _commandDocumentService.GetCommandsDocumentsByCommandId(id_command, limit, offset);
            var CountList = await _commandDocumentService.GetCommandsDocumentsCountByCommandId(id_command);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");
            return Ok(commandsDocuments);
        }

        [HttpGet("{id_commandDocument}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCommandDocumentDto>> GetCommandDocumentById([FromRoute] int id_commandDocument, [FromRoute] int id_command)
        {
            var commandDocument = await _commandDocumentService.GetCommandDocumentById(id_commandDocument, id_command);
            return Ok(commandDocument);
        }
        
        [HttpGet("{id_commandDocument}/download")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DownloadCommandDocument([FromRoute] int id_commandDocument, [FromRoute] int id_command)
        {
            var commandDocument = await _commandDocumentService.GetCommandDocumentById(id_commandDocument, id_command);
            var result = await _fileService.GetFile(commandDocument.url_command_document);
            if (result.Success && result.FileStream != null)
            {
                return File(result.FileStream, result.MimeType, commandDocument.name_command_document);
            }
            else
            {
                return NotFound(result.ErrorMessage);
            }
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCommandDocumentDto>> CreateCommandDocument([FromForm] CreateCommandDocumentByCommandDto commandDocumentDto, [FromRoute] int id_command)
        {
            var commandDocumentDtoFull = new CreateCommandDocumentDto
            {
                id_command = id_command,
                name_command_document = commandDocumentDto.name_command_document,
                document = commandDocumentDto.document
            };
            var commandDocument = await _commandDocumentService.CreateCommandDocument(commandDocumentDtoFull);
            return CreatedAtAction(nameof(GetCommandDocumentById), new { id_commandDocument = commandDocument.id_command_document, id_command = commandDocument.id_command }, commandDocument);
        }

        [HttpPut("{id_commandDocument}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCommandDocumentDto>> UpdateCommandDocument([FromRoute] int id_commandDocument, [FromBody] UpdateCommandDocumentDto commandDocumentDto, [FromRoute] int id_command)
        {
            var commandDocument = await _commandDocumentService.UpdateCommandDocument(id_commandDocument, commandDocumentDto, id_command);
            return Ok(commandDocument);
        }

        [HttpDelete("{id_commandDocument}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteCommandDocument([FromRoute] int id_commandDocument, [FromRoute] int id_command)
        {
            await _commandDocumentService.DeleteCommandDocument(id_commandDocument, id_command);
            return NoContent();
        }
    }
}