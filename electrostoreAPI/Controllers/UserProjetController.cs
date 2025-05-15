using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ProjetCommentaireService;
using Swashbuckle.AspNetCore.Annotations;

namespace electrostore.Controllers
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
        public async Task<ActionResult<IEnumerable<ReadExtendedProjetCommentaireDto>>> GetProjetCommentairesByUserId([FromRoute] int id_user, [FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projet', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projetCommentaires = await _projetCommentaireService.GetProjetCommentairesByUserId(id_user, limit, offset, expand);
            var CountList = await _projetCommentaireService.GetProjetCommentairesCountByUserId(id_user);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers","X-Total-Count");
            return Ok(projetCommentaires);
        }

        [HttpGet("{id_projet_commentaire}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedProjetCommentaireDto>> GetProjetCommentairesById([FromRoute] int id_user, [FromRoute] int id_projet_commentaire, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projet', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projetCommentaire = await _projetCommentaireService.GetProjetCommentairesById(id_projet_commentaire, id_user, null, expand);
            return Ok(projetCommentaire);
        }

        // no create projet commentaire in user controller

        [HttpPut("{id_projet_commentaire}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetCommentaireDto>> UpdateProjetCommentaire([FromRoute] int id_user, [FromRoute] int id_projet_commentaire, [FromBody] UpdateProjetCommentaireDto projetCommentaireDto)
        {
            var projetCommentaire = await _projetCommentaireService.UpdateProjetCommentaire(id_projet_commentaire, projetCommentaireDto, id_user);
            return Ok(projetCommentaire);
        }

        [HttpDelete("{id_projet_commentaire}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteProjetCommentaire([FromRoute] int id_user, [FromRoute] int id_projet_commentaire)
        {
            await _projetCommentaireService.DeleteProjetCommentaire(id_projet_commentaire, id_user);
            return NoContent();
        }
    }
}