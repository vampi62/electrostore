using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.ProjetCommentaireService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/user/{id_user}/projet_commentaire")]

    public class UserProjetCommentaireController : ControllerBase
    {
        private readonly IProjetCommentaireService _projetCommentaireService;

        public UserProjetCommentaireController(IProjetCommentaireService projetCommentaireService)
        {
            _projetCommentaireService = projetCommentaireService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedProjetCommentaireDto>>> GetProjetCommentairesByUserId([FromRoute] int id_user, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projet', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'projet_id==1;content_project_comment=like=comment'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'created_at,asc' or 'created_at,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var projetCommentaires = await _projetCommentaireService.GetProjetCommentairesByUserId(id_user, limit, offset, rsqlDto, sortDto, expand);
            return Ok(projetCommentaires);
        }

        [HttpGet("{id_project_comment}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedProjetCommentaireDto>> GetProjetCommentairesById([FromRoute] int id_user, [FromRoute] int id_project_comment,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projet', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projetCommentaire = await _projetCommentaireService.GetProjetCommentairesById(id_project_comment, id_user, null, expand);
            return Ok(projetCommentaire);
        }

        // no create projet commentaire in user controller

        [HttpPut("{id_project_comment}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetCommentaireDto>> UpdateProjetCommentaire([FromRoute] int id_user, [FromRoute] int id_project_comment, [FromBody] UpdateProjetCommentaireDto projetCommentaireDto)
        {
            var projetCommentaire = await _projetCommentaireService.UpdateProjetCommentaire(id_project_comment, projetCommentaireDto, id_user);
            return Ok(projetCommentaire);
        }

        [HttpDelete("{id_project_comment}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteProjetCommentaire([FromRoute] int id_user, [FromRoute] int id_project_comment)
        {
            await _projetCommentaireService.DeleteProjetCommentaire(id_project_comment, id_user);
            return NoContent();
        }
    }
}