using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.ProjectCommentService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/project/{id_project}/comment")]

    public class ProjectCommentController : ControllerBase
    {
        private readonly IProjectCommentService _projetCommentaireService;

        public ProjectCommentController(IProjectCommentService projetCommentaireService)
        {
            _projetCommentaireService = projetCommentaireService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedProjectCommentDto>>> GetProjetCommentairesByProjetId([FromRoute] int id_project, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'project', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
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
        public async Task<ActionResult<ReadExtendedProjectCommentDto>> GetProjetCommentairesById([FromRoute] int id_project, [FromRoute] int id_project_comment,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'project', 'user'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projetCommentaire = await _projetCommentaireService.GetProjetCommentairesById(id_project_comment, null, id_project, expand);
            return Ok(projetCommentaire);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjectCommentDto>> AddProjetCommentaire([FromRoute] int id_project, [FromBody] CreateProjectCommentByProjectDto projetCommentaireDto)
        {
            var projetCommentaireDtoFull = new CreateProjectCommentDto
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
        public async Task<ActionResult<ReadProjectCommentDto>> UpdateProjetCommentaire([FromRoute] int id_project, [FromRoute] int id_project_comment, [FromBody] UpdateProjectCommentDto projetCommentaireDto)
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