using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.CommandCommentaireService;
using Swashbuckle.AspNetCore.Annotations;
using electrostore.Extensions;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/user/{id_user}/command_commentaire")]

    public class UserCommandCommentaireController : ControllerBase
    {
        private readonly ICommandCommentaireService _commandCommentaireService;

        public UserCommandCommentaireController(ICommandCommentaireService commandCommentaireService)
        {
            _commandCommentaireService = commandCommentaireService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedCommandCommentaireDto>>> GetCommandsCommentairesByUserId([FromRoute] int id_user, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'command', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'contenu_command_commentaire=like=example'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'created_at,asc' or 'created_at,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var commandCommentaires = await _commandCommentaireService.GetCommandsCommentairesByUserId(id_user, limit, offset, rsqlDto, sortDto, expand);
            return Ok(commandCommentaires);
        }

        [HttpGet("{id_command_commentaire}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedCommandCommentaireDto>> GetCommandsCommentaireById([FromRoute] int id_user, [FromRoute] int id_command_commentaire,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'command', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var commandCommentaire = await _commandCommentaireService.GetCommandsCommentaireById(id_command_commentaire, id_user, null, expand);
            return Ok(commandCommentaire);
        }

        // no create command commentaire in user controller

        [HttpPut("{id_command_commentaire}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCommandCommentaireDto>> UpdateCommandCommentaire([FromRoute] int id_user, [FromRoute] int id_command_commentaire, [FromBody] UpdateCommandCommentaireDto commandCommentaireDto)
        {
            var commandCommentaire = await _commandCommentaireService.UpdateCommentaire(id_command_commentaire, commandCommentaireDto, id_user);
            return Ok(commandCommentaire);
        }

        [HttpDelete("{id_command_commentaire}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteCommandCommentaire([FromRoute] int id_user, [FromRoute] int id_command_commentaire)
        {
            await _commandCommentaireService.DeleteCommentaire(id_command_commentaire, id_user);
            return NoContent();
        }
    }
}