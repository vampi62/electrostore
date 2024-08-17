using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.CommandCommentaireService;

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
            await _commandCommentaireService.DeleteCommentaire(id_commandcommentaire, id_user);
            return NoContent();
        }
    }
}