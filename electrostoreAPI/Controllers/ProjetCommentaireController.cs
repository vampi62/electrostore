using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.ProjetCommentaireService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/projet/{id_project}/commentaire")]

    public class ProjetCommentaireController : ControllerBase
    {
        private readonly IProjetCommentaireService _projetCommentaireService;

        public ProjetCommentaireController(IProjetCommentaireService projetCommentaireService)
        {
            _projetCommentaireService = projetCommentaireService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedProjetCommentaireDto>>> GetProjetCommentairesByProjetId([FromRoute] int id_project, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projet', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL filter. Example: 'content_project_comment=like=test'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string. Example: 'created_at,asc' or 'created_at,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var projetCommentaires = await _projetCommentaireService.GetProjetCommentairesByProjetId(id_project, limit, offset, rsqlDto, sortDto, expand);
            return Ok(projetCommentaires);
        }

        [HttpGet("{id_project_comment}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedProjetCommentaireDto>> GetProjetCommentairesById([FromRoute] int id_project, [FromRoute] int id_project_comment,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projet', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projetCommentaire = await _projetCommentaireService.GetProjetCommentairesById(id_project_comment, null, id_project, expand);
            return Ok(projetCommentaire);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetCommentaireDto>> AddProjetCommentaire([FromRoute] int id_project, [FromBody] CreateProjetCommentaireByProjetDto projetCommentaireDto)
        {
            var projetCommentaireDtoFull = new CreateProjetCommentaireDto
            {
                id_project = id_project,
                id_user = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : throw new InvalidOperationException("User identifier not found."),
                content_project_comment = projetCommentaireDto.content_project_comment
            };
            var projetCommentaire = await _projetCommentaireService.CreateProjetCommentaire(projetCommentaireDtoFull);
            return CreatedAtAction(nameof(GetProjetCommentairesById), new { id_project = projetCommentaire.id_project, id_project_comment = projetCommentaire.id_project_comment }, projetCommentaire);
        }

        [HttpPut("{id_project_comment}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetCommentaireDto>> UpdateProjetCommentaire([FromRoute] int id_project, [FromRoute] int id_project_comment, [FromBody] UpdateProjetCommentaireDto projetCommentaireDto)
        {
            var projetCommentaire = await _projetCommentaireService.UpdateProjetCommentaire(id_project_comment, projetCommentaireDto, null, id_project);
            return Ok(projetCommentaire);
        }

        [HttpDelete("{id_project_comment}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteProjetCommentaire([FromRoute] int id_project, [FromRoute] int id_project_comment)
        {
            await _projetCommentaireService.DeleteProjetCommentaire(id_project_comment, null, id_project);
            return NoContent();
        }
    }
}