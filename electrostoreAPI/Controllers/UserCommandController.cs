using Microsoft.AspNetCore.Mvc;
using electrostore.Dto;
using electrostore.Services.CommandCommentaireService;
using System.Security.Claims;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/user/{id_user}/commandcommentaire")]

    public class UserCommandCommentaireController : ControllerBase
    {
        private readonly ICommandCommentaireService _commandCommentaireService;

        public UserCommandCommentaireController(ICommandCommentaireService commandCommentaireService)
        {
            _commandCommentaireService = commandCommentaireService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReadCommandCommentaireDto>>> GetCommandsCommentairesByUserId([FromRoute] int id_user, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            if (!User.IsInRole("Admin") && id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to access this resource" });
            }
            var commandCommentaires = await _commandCommentaireService.GetCommandsCommentairesByUserId(id_user, limit, offset);
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
        public async Task<ActionResult<ReadCommandCommentaireDto>> GetCommandsCommentaireById([FromRoute] int id_user, [FromRoute] int id_commandcommentaire)
        {
            if (!User.IsInRole("Admin") && id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to access this resource" });
            }
            var commandCommentaire = await _commandCommentaireService.GetCommandsCommentaireById(id_commandcommentaire, id_user);
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

        // no create command commentaire in user controller

        [HttpPut("{id_commandcommentaire}")]
        public async Task<ActionResult<ReadCommandCommentaireDto>> UpdateCommandCommentaire([FromRoute] int id_user, [FromRoute] int id_commandcommentaire, [FromBody] UpdateCommandCommentaireDto commandCommentaireDto)
        {
            if (!User.IsInRole("Admin") && id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to access this resource" });
            }
            var commandCommentaire = await _commandCommentaireService.UpdateCommentaire(id_commandcommentaire, commandCommentaireDto, id_user);
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
        public async Task<ActionResult> DeleteCommandCommentaire([FromRoute] int id_user, [FromRoute] int id_commandcommentaire)
        {
            if (!User.IsInRole("Admin") && id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to access this resource" });
            }
            await _commandCommentaireService.DeleteCommentaire(id_commandcommentaire, id_user);
            return NoContent();
        }
    }
}