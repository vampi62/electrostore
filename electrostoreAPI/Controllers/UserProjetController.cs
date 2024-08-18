using Microsoft.AspNetCore.Mvc;
using electrostore.Dto;
using electrostore.Services.ProjetCommentaireService;
using System.Security.Claims;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/user/{id_user}/projetcommentaire")]

    public class UserProjetController : ControllerBase
    {
        private readonly IProjetCommentaireService _projetCommentaireService;

        public UserProjetController(IProjetCommentaireService projetCommentaireService)
        {
            _projetCommentaireService = projetCommentaireService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReadProjetCommentaireDto>>> GetProjetCommentairesByUserId([FromRoute] int id_user, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            if (!User.IsInRole("Admin") && id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to access this resource" });
            }
            var projetCommentaires = await _projetCommentaireService.GetProjetCommentairesByUserId(id_user, limit, offset);
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
        public async Task<ActionResult<ReadProjetCommentaireDto>> GetProjetCommentairesByCommentaireId([FromRoute] int id_user, [FromRoute] int id_projetcommentaire)
        {
            if (!User.IsInRole("Admin") && id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to access this resource" });
            }
            var projetCommentaire = await _projetCommentaireService.GetProjetCommentairesByCommentaireId(id_projetcommentaire, id_user);
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

        // no create projet commentaire in user controller

        [HttpPut("{id_projetcommentaire}")]
        public async Task<ActionResult<ReadProjetCommentaireDto>> UpdateProjetCommentaire([FromRoute] int id_user, [FromRoute] int id_projetcommentaire, [FromBody] UpdateProjetCommentaireDto projetCommentaireDto)
        {
            if (!User.IsInRole("Admin") && id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to access this resource" });
            }
            var projetCommentaire = await _projetCommentaireService.UpdateProjetCommentaire(id_projetcommentaire, projetCommentaireDto, id_user);
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
        public async Task<ActionResult> DeleteProjetCommentaire([FromRoute] int id_user, [FromRoute] int id_projetcommentaire)
        {
            if (!User.IsInRole("Admin") && id_user != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""))
            {
                return Unauthorized(new { message = "You are not allowed to access this resource" });
            }
            await _projetCommentaireService.DeleteProjetCommentaire(id_projetcommentaire, id_user);
            return NoContent();
        }
    }
}