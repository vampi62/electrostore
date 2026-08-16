using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.ProjetService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
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
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'project_comments', 'project_documents', 'project_items', 'project_tags', 'project_status_history'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to select list of ID to research in the base. Multiple values can be specified by separating them with ','.")] List<int>? idResearch = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'name_project=like=example'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'name_project,asc' or 'name_project,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var projets = await _projetService.GetProjets(limit, offset, rsqlDto, sortDto, expand, idResearch);
            return Ok(projets);
        }

        [HttpGet("{id_project}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedProjetDto>> GetProjetById([FromRoute] int id_project,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'project_comments', 'project_documents', 'project_items', 'project_tags', 'project_status_history'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projet = await _projetService.GetProjetById(id_project, expand);
            return Ok(projet);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetDto>> CreateProjet([FromBody] CreateProjetDto projetDto)
        {
            var projet = await _projetService.CreateProjet(projetDto);
            return CreatedAtAction(nameof(GetProjetById), new { id_project = projet.id_project }, projet);
        }

        [HttpPut("{id_project}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetDto>> UpdateProjet([FromRoute] int id_project, [FromBody] UpdateProjetDto projetDto)
        {
            var projet = await _projetService.UpdateProjet(id_project, projetDto);
            return Ok(projet);
        }

        [HttpDelete("{id_project}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteProjet([FromRoute] int id_project)
        {
            await _projetService.DeleteProjet(id_project);
            return NoContent();
        }
    }
}