using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ProjetService;
using Swashbuckle.AspNetCore.Annotations;
using electrostore.Extensions;

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
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedProjetDto>>> GetProjets([FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projets_commentaires', 'projets_documents', 'projets_items', 'projets_projet_tags', 'projets_status_history'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to select list of ID to research in the base. Multiple values can be specified by separating them with ','.")] List<int>? idResearch = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'nom_projet=like=example'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'nom_projet,asc' or 'nom_projet,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var projets = await _projetService.GetProjets(limit, offset, rsqlDto, sortDto, expand, idResearch);
            return Ok(projets);
        }

        [HttpGet("{id_projet}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedProjetDto>> GetProjetById([FromRoute] int id_projet,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projets_commentaires', 'projets_documents', 'projets_items', 'projets_projet_tags', 'projets_status_history'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projet = await _projetService.GetProjetById(id_projet, expand);
            return Ok(projet);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetDto>> CreateProjet([FromBody] CreateProjetDto projetDto)
        {
            var projet = await _projetService.CreateProjet(projetDto);
            return CreatedAtAction(nameof(GetProjetById), new { id_projet = projet.id_projet }, projet);
        }

        [HttpPut("{id_projet}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetDto>> UpdateProjet([FromRoute] int id_projet, [FromBody] UpdateProjetDto projetDto)
        {
            var projet = await _projetService.UpdateProjet(id_projet, projetDto);
            return Ok(projet);
        }

        [HttpDelete("{id_projet}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteProjet([FromRoute] int id_projet)
        {
            await _projetService.DeleteProjet(id_projet);
            return NoContent();
        }
    }
}