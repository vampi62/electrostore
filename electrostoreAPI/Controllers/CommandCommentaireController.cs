using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.CommandCommentaireService;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;

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
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadExtendedCommandCommentaireDto>>> GetCommandsCommentairesByCommandId([FromRoute] int id_command, [FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery, SwaggerParameter(Description = "Fields to expand. Possible values: 'command', 'user'. Multiple values can be specified by separating them with ','. Default: \"\"")] string expand = "")
        {
            var commandCommentaires = await _commandCommentaireService.GetCommandsCommentairesByCommandId(id_command, limit, offset, expand.Split(',').ToList());
            var CountList = await _commandCommentaireService.GetCommandsCommentairesCountByCommandId(id_command);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");
            return Ok(commandCommentaires);
        }

        [HttpGet("{id_command_commentaire}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedCommandCommentaireDto>> GetCommandsCommentaireById([FromRoute] int id_command, [FromRoute] int id_command_commentaire, [FromQuery, SwaggerParameter(Description = "Fields to expand. Possible values: 'command', 'user'. Multiple values can be specified by separating them with ','. Default: \"\"")] string expand = "")
        {
            var commandCommentaire = await _commandCommentaireService.GetCommandsCommentaireById(id_command_commentaire, null, id_command, expand.Split(',').ToList());
            return Ok(commandCommentaire);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCommandCommentaireDto>> CreateCommentaire([FromRoute] int id_command, [FromBody] CreateCommandCommentaireByCommandDto commandCommentaireDto)
        {
            var commandCommentaireDtoFull = new CreateCommandCommentaireDto
            {
                id_command = id_command,
                id_user = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value),
                contenu_command_commentaire = commandCommentaireDto.contenu_command_commentaire
            };
            var commandCommentaire = await _commandCommentaireService.CreateCommentaire(commandCommentaireDtoFull);
            return CreatedAtAction(nameof(GetCommandsCommentaireById), new { id_command = commandCommentaire.id_command, id_command_commentaire = commandCommentaire.id_command_commentaire }, commandCommentaire);
        }

        [HttpPut("{id_command_commentaire}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCommandCommentaireDto>> UpdateCommentaire([FromRoute] int id_command, [FromRoute] int id_command_commentaire, [FromBody] UpdateCommandCommentaireDto commandCommentaireDto)
        {
            var checkCommandCommentaire = await _commandCommentaireService.GetCommandsCommentaireById(id_command_commentaire, null, id_command);
            if (!User.IsInRole("admin") && checkCommandCommentaire.id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to access this resource" });
            }
            var commandCommentaire = await _commandCommentaireService.UpdateCommentaire(id_command_commentaire, commandCommentaireDto, null, id_command);
            return Ok(commandCommentaire);
        }

        [HttpDelete("{id_command_commentaire}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteCommentaire([FromRoute] int id_command, [FromRoute] int id_command_commentaire)
        {
            var checkCommandCommentaire = await _commandCommentaireService.GetCommandsCommentaireById(id_command_commentaire, null, id_command);
            if (!User.IsInRole("admin") && checkCommandCommentaire.id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to access this resource" });
            }
            await _commandCommentaireService.DeleteCommentaire(id_command_commentaire, null, id_command);
            return NoContent();
        }
    }
}