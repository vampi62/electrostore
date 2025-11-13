using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ProjetCommentaireService;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/projet/{id_projet}/commentaire")]

    public class ProjetCommentaireController : ControllerBase
    {
        private readonly IProjetCommentaireService _projetCommentaireService;

        public ProjetCommentaireController(IProjetCommentaireService projetCommentaireService)
        {
            _projetCommentaireService = projetCommentaireService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadExtendedProjetCommentaireDto>>> GetProjetCommentairesByProjetId([FromRoute] int id_projet, [FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projet', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projetCommentaires = await _projetCommentaireService.GetProjetCommentairesByProjetId(id_projet, limit, offset, expand);
            var CountList = await _projetCommentaireService.GetProjetCommentairesCountByProjetId(id_projet);
            Response.Headers["X-Total-Count"] = CountList.ToString();
            Response.Headers.AccessControlExposeHeaders = "X-Total-Count";
            return Ok(projetCommentaires);
        }

        [HttpGet("{id_projet_commentaire}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedProjetCommentaireDto>> GetProjetCommentairesById([FromRoute] int id_projet, [FromRoute] int id_projet_commentaire, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projet', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projetCommentaire = await _projetCommentaireService.GetProjetCommentairesById(id_projet_commentaire, null, id_projet, expand);
            return Ok(projetCommentaire);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetCommentaireDto>> AddProjetCommentaire([FromRoute] int id_projet, [FromBody] CreateProjetCommentaireByProjetDto projetCommentaireDto)
        {
            var projetCommentaireDtoFull = new CreateProjetCommentaireDto
            {
                id_projet = id_projet,
                id_user = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : throw new InvalidOperationException("User identifier not found."),
                contenu_projet_commentaire = projetCommentaireDto.contenu_projet_commentaire
            };
            var projetCommentaire = await _projetCommentaireService.CreateProjetCommentaire(projetCommentaireDtoFull);
            return CreatedAtAction(nameof(GetProjetCommentairesById), new { id_projet = projetCommentaire.id_projet, id_projet_commentaire = projetCommentaire.id_projet_commentaire }, projetCommentaire);
        }

        [HttpPut("{id_projet_commentaire}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetCommentaireDto>> UpdateProjetCommentaire([FromRoute] int id_projet, [FromRoute] int id_projet_commentaire, [FromBody] UpdateProjetCommentaireDto projetCommentaireDto)
        {
            var projetCommentaire = await _projetCommentaireService.UpdateProjetCommentaire(id_projet_commentaire, projetCommentaireDto, null, id_projet);
            return Ok(projetCommentaire);
        }

        [HttpDelete("{id_projet_commentaire}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteProjetCommentaire([FromRoute] int id_projet, [FromRoute] int id_projet_commentaire)
        {
            await _projetCommentaireService.DeleteProjetCommentaire(id_projet_commentaire, null, id_projet);
            return NoContent();
        }
    }
}