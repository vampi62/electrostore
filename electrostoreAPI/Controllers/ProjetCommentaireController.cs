using Microsoft.AspNetCore.Mvc;
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
        public async Task<ActionResult<IEnumerable<ReadProjetCommentaireDto>>> GetProjetCommentairesByProjetId([FromRoute] int id_projet, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var projetCommentaires = await _projetCommentaireService.GetProjetCommentairesByProjetId(id_projet, limit, offset);
            if (projetCommentaires.Result is BadRequestObjectResult)
            {
                return projetCommentaires.Result;
            }
            if (projetCommentaires.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(projetCommentaires.Value);
        }

        [HttpGet("{id_projetcommentaire}")]
        public async Task<ActionResult<ReadProjetCommentaireDto>> GetProjetCommentairesByCommentaireId([FromRoute] int id_projet, [FromRoute] int id_projetcommentaire)
        {
            var projetCommentaire = await _projetCommentaireService.GetProjetCommentairesByCommentaireId(id_projetcommentaire, null, id_projet);
            if (projetCommentaire.Result is BadRequestObjectResult)
            {
                return projetCommentaire.Result;
            }
            if (projetCommentaire.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(projetCommentaire.Value);
        }

        [HttpPost]
        public async Task<ActionResult<ReadProjetCommentaireDto>> AddProjetCommentaire([FromRoute] int id_projet, [FromBody] CreateProjetCommentaireByProjetDto projetCommentaireDto)
        {
            var projetCommentaireDtoFull = new CreateProjetCommentaireDto
            {
                id_projet = id_projet,
                id_user = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value),
                contenu_projetcommentaire = projetCommentaireDto.contenu_projetcommentaire
            };
            var projetCommentaire = await _projetCommentaireService.CreateProjetCommentaire(projetCommentaireDtoFull);
            if (projetCommentaire.Result is BadRequestObjectResult)
            {
                return projetCommentaire.Result;
            }
            if (projetCommentaire.Value == null)
            {
                return StatusCode(500);
            }
            return CreatedAtAction(nameof(GetProjetCommentairesByCommentaireId), new { id_projet = projetCommentaire.Value.id_projet, id_projetcommentaire = projetCommentaire.Value.id_projetcommentaire }, projetCommentaire.Value);
        }

        [HttpPut("{id_projetcommentaire}")]
        public async Task<ActionResult<ReadProjetCommentaireDto>> UpdateProjetCommentaire([FromRoute] int id_projet, [FromRoute] int id_projetcommentaire, [FromBody] UpdateProjetCommentaireDto projetCommentaireDto)
        {
            var checkProjetCommentaire = await _projetCommentaireService.GetProjetCommentairesByCommentaireId(id_projetcommentaire, null, id_projet);
            if (checkProjetCommentaire.Result is BadRequestObjectResult)
            {
                return checkProjetCommentaire.Result;
            }
            if (checkProjetCommentaire.Value == null)
            {
                return StatusCode(500);
            }
            if (!User.IsInRole("admin") && checkProjetCommentaire.Value.id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to access this resource" });
            }
            var projetCommentaire = await _projetCommentaireService.UpdateProjetCommentaire(id_projetcommentaire, projetCommentaireDto, null, id_projet);
            if (projetCommentaire.Result is BadRequestObjectResult)
            {
                return projetCommentaire.Result;
            }
            if (projetCommentaire.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(projetCommentaire.Value);
        }

        [HttpDelete("{id_projetcommentaire}")]
        public async Task<ActionResult> DeleteProjetCommentaire([FromRoute] int id_projet, [FromRoute] int id_projetcommentaire)
        {
            var checkProjetCommentaire = await _projetCommentaireService.GetProjetCommentairesByCommentaireId(id_projetcommentaire, null, id_projet);
            if (checkProjetCommentaire.Result is BadRequestObjectResult)
            {
                return checkProjetCommentaire.Result;
            }
            if (checkProjetCommentaire.Value == null)
            {
                return StatusCode(500);
            }
            if (!User.IsInRole("admin") && checkProjetCommentaire.Value.id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to access this resource" });
            }
            await _projetCommentaireService.DeleteProjetCommentaire(id_projetcommentaire, null, id_projet);
            return NoContent();
        }
    }
}