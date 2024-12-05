using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.CommandDocumentService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/command/{id_command}/document")]

    public class CommandDocumentController : ControllerBase
    {
        private readonly ICommandDocumentService _commandDocumentService;

        public CommandDocumentController(ICommandDocumentService commandDocumentService)
        {
            _commandDocumentService = commandDocumentService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadCommandDocumentDto>>> GetCommandsDocumentsByCommandId([FromRoute] int id_command, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var commandsDocuments = await _commandDocumentService.GetCommandDocumentsByCommandId(id_command, limit, offset);
            var CountList = await _commandDocumentService.GetCommandDocumentsCountByCommandId(id_command);
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
            var result = await _commandDocumentService.GetFile(commandDocument.url_command_document); // check if commandDocument.url_command_document is a valid path
            if (result.Success)
            {
                return PhysicalFile(result.FilePath, result.MimeType, commandDocument.name_command_document);
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
                type_command_document = commandDocumentDto.type_command_document,
                document = commandDocumentDto.document
            };
            var commandDocument = await _commandDocumentService.CreateCommandDocument(commandDocumentDtoFull);
            return CreatedAtAction(nameof(GetCommandDocumentById), new { id_commandDocument = commandDocument.id_command_document, id_command = commandDocument.id_command }, commandDocument);
        }

        [HttpPut("{id_commandDocument}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCommandDocumentDto>> UpdateCommandDocument([FromRoute] int id_commandDocument, [FromForm] UpdateCommandDocumentDto commandDocumentDto, [FromRoute] int id_command)
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