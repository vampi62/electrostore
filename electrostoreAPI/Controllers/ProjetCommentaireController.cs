using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ProjetCommentaireService;

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
            return Ok(projetCommentaires);
        }

        [HttpGet("{id_projetcommentaire}")]
        public async Task<ActionResult<ReadProjetCommentaireDto>> GetProjetCommentairesByCommentaireId([FromRoute] int id_projet, [FromRoute] int id_projetcommentaire)
        {
            var projetCommentaire = await _projetCommentaireService.GetProjetCommentairesByCommentaireId(id_projetcommentaire, null, id_projet);
            return Ok(projetCommentaire);
        }

        [HttpPost]
        public async Task<ActionResult<ReadProjetCommentaireDto>> AddProjetCommentaire([FromRoute] int id_projet, [FromBody] CreateProjetCommentaireByProjetDto projetCommentaireDto)
        {
            var projetCommentaireDtoFull = new CreateProjetCommentaireDto
            {
                id_projet = id_projet,
                id_user = projetCommentaireDto.id_user,
                contenu_projetcommentaire = projetCommentaireDto.contenu_projetcommentaire
            };
            var projetCommentaire = await _projetCommentaireService.CreateProjetCommentaire(projetCommentaireDtoFull);
            return CreatedAtAction(nameof(GetProjetCommentairesByCommentaireId), new { id_projet = projetCommentaire.id_projet, id_projetcommentaire = projetCommentaire.id_projetcommentaire }, projetCommentaire);
        }

        [HttpPut("{id_projetcommentaire}")]
        public async Task<ActionResult<ReadProjetCommentaireDto>> UpdateProjetCommentaire([FromRoute] int id_projet, [FromRoute] int id_projetcommentaire, [FromBody] UpdateProjetCommentaireDto projetCommentaireDto)
        {
            var projetCommentaire = await _projetCommentaireService.UpdateProjetCommentaire(id_projetcommentaire, projetCommentaireDto, null, id_projet);
            return Ok(projetCommentaire);
        }

        [HttpDelete("{id_projetcommentaire}")]
        public async Task<ActionResult> DeleteProjetCommentaire([FromRoute] int id_projet, [FromRoute] int id_projetcommentaire)
        {
            await _projetCommentaireService.DeleteProjetCommentaire(id_projetcommentaire, null, id_projet);
            return Ok();
        }
    }
}