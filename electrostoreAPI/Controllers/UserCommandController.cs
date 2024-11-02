using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadCommandCommentaireDto>>> GetCommandsCommentairesByUserId([FromRoute] int id_user, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            if (!User.IsInRole("admin") && id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to access this resource" });
            }
            var commandCommentaires = await _commandCommentaireService.GetCommandsCommentairesByUserId(id_user, limit, offset);
            return Ok(commandCommentaires);
        }

        [HttpGet("{id_commandcommentaire}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCommandCommentaireDto>> GetCommandsCommentaireById([FromRoute] int id_user, [FromRoute] int id_commandcommentaire)
        {
            if (!User.IsInRole("admin") && id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to access this resource" });
            }
            var commandCommentaire = await _commandCommentaireService.GetCommandsCommentaireById(id_commandcommentaire, id_user);
            return Ok(commandCommentaire);
        }

        // no create command commentaire in user controller

        [HttpPut("{id_commandcommentaire}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCommandCommentaireDto>> UpdateCommandCommentaire([FromRoute] int id_user, [FromRoute] int id_commandcommentaire, [FromBody] UpdateCommandCommentaireDto commandCommentaireDto)
        {
            if (!User.IsInRole("admin") && id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to access this resource" });
            }
            var commandCommentaire = await _commandCommentaireService.UpdateCommentaire(id_commandcommentaire, commandCommentaireDto, id_user);
            return Ok(commandCommentaire);
        }

        [HttpDelete("{id_commandcommentaire}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteCommandCommentaire([FromRoute] int id_user, [FromRoute] int id_commandcommentaire)
        {
            if (!User.IsInRole("admin") && id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to access this resource" });
            }
            await _commandCommentaireService.DeleteCommentaire(id_commandcommentaire, id_user);
            return NoContent();
        }
    }
}