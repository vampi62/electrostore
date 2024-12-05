using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ProjetCommentaireService;
using System.Security.Claims;

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
        public async Task<ActionResult<IEnumerable<ReadProjetCommentaireDto>>> GetProjetCommentairesByProjetId([FromRoute] int id_projet, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var projetCommentaires = await _projetCommentaireService.GetProjetCommentairesByProjetId(id_projet, limit, offset);
            var CountList = await _projetCommentaireService.GetProjetCommentairesCountByProjetId(id_projet);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");
            return Ok(projetCommentaires);
        }

        [HttpGet("{id_projet_commentaire}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetCommentaireDto>> GetProjetCommentairesByCommentaireId([FromRoute] int id_projet, [FromRoute] int id_projet_commentaire)
        {
            var projetCommentaire = await _projetCommentaireService.GetProjetCommentairesByCommentaireId(id_projet_commentaire, null, id_projet);
            return Ok(projetCommentaire);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetCommentaireDto>> AddProjetCommentaire([FromRoute] int id_projet, [FromBody] CreateProjetCommentaireByProjetDto projetCommentaireDto)
        {
            var projetCommentaireDtoFull = new CreateProjetCommentaireDto
            {
                id_projet = id_projet,
                id_user = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value),
                contenu_projet_commentaire = projetCommentaireDto.contenu_projet_commentaire
            };
            var projetCommentaire = await _projetCommentaireService.CreateProjetCommentaire(projetCommentaireDtoFull);
            return CreatedAtAction(nameof(GetProjetCommentairesByCommentaireId), new { id_projet = projetCommentaire.id_projet, id_projet_commentaire = projetCommentaire.id_projet_commentaire }, projetCommentaire);
        }

        [HttpPut("{id_projet_commentaire}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetCommentaireDto>> UpdateProjetCommentaire([FromRoute] int id_projet, [FromRoute] int id_projet_commentaire, [FromBody] UpdateProjetCommentaireDto projetCommentaireDto)
        {
            var checkProjetCommentaire = await _projetCommentaireService.GetProjetCommentairesByCommentaireId(id_projet_commentaire, null, id_projet);
            if (!User.IsInRole("admin") && checkProjetCommentaire.id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to access this resource" });
            }
            var projetCommentaire = await _projetCommentaireService.UpdateProjetCommentaire(id_projet_commentaire, projetCommentaireDto, null, id_projet);
            return Ok(projetCommentaire);
        }

        [HttpDelete("{id_projet_commentaire}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteProjetCommentaire([FromRoute] int id_projet, [FromRoute] int id_projet_commentaire)
        {
            var checkProjetCommentaire = await _projetCommentaireService.GetProjetCommentairesByCommentaireId(id_projet_commentaire, null, id_projet);
            if (!User.IsInRole("admin") && checkProjetCommentaire.id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to access this resource" });
            }
            await _projetCommentaireService.DeleteProjetCommentaire(id_projet_commentaire, null, id_projet);
            return NoContent();
        }
    }
}