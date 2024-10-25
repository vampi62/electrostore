using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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
        [Authorize(Policy = "AccessTokenPolicy")]
        public async Task<ActionResult<IEnumerable<ReadCommandCommentaireDto>>> GetCommandsCommentairesByCommandId([FromRoute] int id_command, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var commandCommentaires = await _commandCommentaireService.GetCommandsCommentairesByCommandId(id_command, limit, offset);
            return Ok(commandCommentaires);
        }

        [HttpGet("{id_commandcommentaire}")]
        [Authorize(Policy = "AccessTokenPolicy")]
        public async Task<ActionResult<ReadCommandCommentaireDto>> GetCommandsCommentaireById([FromRoute] int id_command, [FromRoute] int id_commandcommentaire)
        {
            var commandCommentaire = await _commandCommentaireService.GetCommandsCommentaireById(id_commandcommentaire, null, id_command);
            return Ok(commandCommentaire);
        }

        [HttpPost]
        [Authorize(Policy = "AccessTokenPolicy")]
        public async Task<ActionResult<ReadCommandCommentaireDto>> CreateCommentaire([FromRoute] int id_command, [FromBody] CreateCommandCommentaireByCommandDto commandCommentaireDto)
        {
            var commandCommentaireDtoFull = new CreateCommandCommentaireDto
            {
                id_command = id_command,
                id_user = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value),
                contenu_commandcommentaire = commandCommentaireDto.contenu_commandcommentaire
            };
            var commandCommentaire = await _commandCommentaireService.CreateCommentaire(commandCommentaireDtoFull);
            return CreatedAtAction(nameof(GetCommandsCommentaireById), new { id_command = commandCommentaire.id_command, id_commandcommentaire = commandCommentaire.id_commandcommentaire }, commandCommentaire);
        }

        [HttpPut("{id_commandcommentaire}")]
        [Authorize(Policy = "AccessTokenPolicy")]
        public async Task<ActionResult<ReadCommandCommentaireDto>> UpdateCommentaire([FromRoute] int id_command, [FromRoute] int id_commandcommentaire, [FromBody] UpdateCommandCommentaireDto commandCommentaireDto)
        {
            var checkCommandCommentaire = await _commandCommentaireService.GetCommandsCommentaireById(id_commandcommentaire, null, id_command);
            if (!User.IsInRole("admin") && checkCommandCommentaire.id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to access this resource" });
            }
            var commandCommentaire = await _commandCommentaireService.UpdateCommentaire(id_commandcommentaire, commandCommentaireDto, null, id_command);
            return Ok(commandCommentaire);
        }

        [HttpDelete("{id_commandcommentaire}")]
        [Authorize(Policy = "AccessTokenPolicy")]
        public async Task<ActionResult> DeleteCommentaire([FromRoute] int id_command, [FromRoute] int id_commandcommentaire)
        {
            var checkCommandCommentaire = await _commandCommentaireService.GetCommandsCommentaireById(id_commandcommentaire, null, id_command);
            if (!User.IsInRole("admin") && checkCommandCommentaire.id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to access this resource" });
            }
            await _commandCommentaireService.DeleteCommentaire(id_commandcommentaire, null, id_command);
            return NoContent();
        }
    }
}