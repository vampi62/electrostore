using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ProjetService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/projet")]

    public class ProjetController : ControllerBase
    {
        private readonly IProjetService _projetService;

        public ProjetController(IProjetService projetService)
        {
            _projetService = projetService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReadProjetDto>>> GetProjets([FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var projets = await _projetService.GetProjets(limit, offset);
            return Ok(projets);
        }

        [HttpGet("{id_projet}")]
        public async Task<ActionResult<ReadProjetDto>> GetProjetById([FromRoute] int id_projet)
        {
            var projet = await _projetService.GetProjetById(id_projet);
            return Ok(projet);
        }

        [HttpPost]
        public async Task<ActionResult<ReadProjetDto>> AddProjet([FromBody] CreateProjetDto projetDto)
        {
            var projet = await _projetService.CreateProjet(projetDto);
            return CreatedAtAction(nameof(GetProjetById), new { id_projet = projet.id_projet }, projet);
        }

        [HttpPut("{id_projet}")]
        public async Task<ActionResult<ReadProjetDto>> UpdateProjet([FromRoute] int id_projet, [FromBody] UpdateProjetDto projetDto)
        {
            var projet = await _projetService.UpdateProjet(id_projet, projetDto);
            return Ok(projet);
        }

        [HttpDelete("{id_projet}")]
        public async Task<ActionResult> DeleteProjet([FromRoute] int id_projet)
        {
            await _projetService.DeleteProjet(id_projet);
            return Ok();
        }
    }
}