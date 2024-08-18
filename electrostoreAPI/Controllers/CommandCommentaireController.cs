using Microsoft.AspNetCore.Mvc;
using electrostore.Dto;
using electrostore.Services.CommandCommentaireService;
using System.Security.Claims;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/command/{id_command}/commentaire")]

    public class CommandCommentaireController : ControllerBase
    {
        private readonly ICommandCommentaireService _commandCommentaireService;

        public CommandCommentaireController(ICommandCommentaireService commandCommentaireService)
        {
            _commandCommentaireService = commandCommentaireService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReadCommandCommentaireDto>>> GetCommandsCommentairesByCommandId([FromRoute] int id_command, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var commandCommentaires = await _commandCommentaireService.GetCommandsCommentairesByCommandId(id_command, limit, offset);
            if (commandCommentaires.Result is BadRequestObjectResult)
            {
                return commandCommentaires.Result;
            }
            if (commandCommentaires.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(commandCommentaires.Value);
        }

        [HttpGet("{id_commandcommentaire}")]
        public async Task<ActionResult<ReadCommandCommentaireDto>> GetCommandsCommentaireById([FromRoute] int id_command, [FromRoute] int id_commandcommentaire)
        {
            var commandCommentaire = await _commandCommentaireService.GetCommandsCommentaireById(id_commandcommentaire, null, id_command);
            if (commandCommentaire.Result is BadRequestObjectResult)
            {
                return commandCommentaire.Result;
            }
            if (commandCommentaire.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(commandCommentaire.Value);
        }

        [HttpPost]
        public async Task<ActionResult<ReadCommandCommentaireDto>> CreateCommentaire([FromRoute] int id_command, [FromBody] CreateCommandCommentaireByCommandDto commandCommentaireDto)
        {
            var commandCommentaireDtoFull = new CreateCommandCommentaireDto
            {
                id_command = id_command,
                id_user = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value),
                contenu_commandcommentaire = commandCommentaireDto.contenu_commandcommentaire
            };
            var commandCommentaire = await _commandCommentaireService.CreateCommentaire(commandCommentaireDtoFull);
            if (commandCommentaire.Result is BadRequestObjectResult)
            {
                return commandCommentaire.Result;
            }
            if (commandCommentaire.Value == null)
            {
                return StatusCode(500);
            }
            return CreatedAtAction(nameof(GetCommandsCommentaireById), new { id_command = commandCommentaire.Value.id_command, id_commandcommentaire = commandCommentaire.Value.id_commandcommentaire }, commandCommentaire.Value);
        }

        [HttpPut("{id_commandcommentaire}")]
        public async Task<ActionResult<ReadCommandCommentaireDto>> UpdateCommentaire([FromRoute] int id_command, [FromRoute] int id_commandcommentaire, [FromBody] UpdateCommandCommentaireDto commandCommentaireDto)
        {
            var checkCommandCommentaire = await _commandCommentaireService.GetCommandsCommentaireById(id_commandcommentaire, null, id_command);
            if (checkCommandCommentaire.Result is BadRequestObjectResult)
            {
                return checkCommandCommentaire.Result;
            }
            if (checkCommandCommentaire.Value == null)
            {
                return StatusCode(500);
            }
            if (!User.IsInRole("Admin") && checkCommandCommentaire.Value.id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to access this resource" });
            }
            var commandCommentaire = await _commandCommentaireService.UpdateCommentaire(id_commandcommentaire, commandCommentaireDto, null, id_command);
            if (commandCommentaire.Result is BadRequestObjectResult)
            {
                return commandCommentaire.Result;
            }
            if (commandCommentaire.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(commandCommentaire.Value);
        }

        [HttpDelete("{id_commandcommentaire}")]
        public async Task<ActionResult> DeleteCommentaire([FromRoute] int id_command, [FromRoute] int id_commandcommentaire)
        {
            var checkCommandCommentaire = await _commandCommentaireService.GetCommandsCommentaireById(id_commandcommentaire, null, id_command);
            if (checkCommandCommentaire.Result is BadRequestObjectResult)
            {
                return checkCommandCommentaire.Result;
            }
            if (checkCommandCommentaire.Value == null)
            {
                return StatusCode(500);
            }
            if (!User.IsInRole("Admin") && checkCommandCommentaire.Value.id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to access this resource" });
            }
            await _commandCommentaireService.DeleteCommentaire(id_commandcommentaire, null, id_command);
            return NoContent();
        }
    }
}