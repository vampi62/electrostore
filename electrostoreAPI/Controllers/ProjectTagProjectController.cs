using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.ProjectProjectTagService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/project-tag/{id_project_tag}/project")]

    public class ProjectTagProjectController : ControllerBase
    {
        private readonly IProjectProjectTagService _projetProjetTagService;

        public ProjectTagProjectController(IProjectProjectTagService projetProjetTagService)
        {
            _projetProjetTagService = projetProjetTagService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedProjectProjectTagDto>>> GetProjetsProjetTagsByprojetTagId([FromRoute] int id_project_tag, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'project_tag', 'project'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL filter. Example: 'id_project==5'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string. Example: 'id_project,asc' or 'id_project,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var projetProjetTags = await _projetProjetTagService.GetProjetsProjetTagsByprojetTagId(id_project_tag, limit, offset, rsqlDto, sortDto, expand);
            return Ok(projetProjetTags);
        }

        [HttpGet("{id_project}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedProjectProjectTagDto>> GetProjetProjetTagById([FromRoute] int id_project_tag, [FromRoute] int id_project,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'project_tag', 'project'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projetProjetTag = await _projetProjetTagService.GetProjetProjetTagById(id_project, id_project_tag, expand);
            return Ok(projetProjetTag);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjectProjectTagDto>> CreateProjetProjetTag([FromRoute] int id_project_tag, [FromBody] CreateProjectProjectTagByProjectTagDto projetProjetTagDto)
        {
            var projetProjetTagFull = new CreateProjectProjectTagDto
            {
                id_project_tag = id_project_tag,
                id_project = projetProjetTagDto.id_project
            };
            var newProjetProjetTag = await _projetProjetTagService.CreateProjetProjetTag(projetProjetTagFull);
            return CreatedAtAction(nameof(GetProjetProjetTagById), new { id_project_tag = newProjetProjetTag.id_project_tag, id_project = newProjetProjetTag.id_project }, newProjetProjetTag);
        }

        [HttpPost("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkProjectProjectTagDto>> CreateBulkProjetProjetTag([FromRoute] int id_project_tag, [FromBody] List<CreateProjectProjectTagByProjectTagDto> projetProjetTagsDto)
        {
            var projetProjetTagsDtoFull = projetProjetTagsDto.Select(projetProjetTagDto => new CreateProjectProjectTagDto
            {
                id_project_tag = id_project_tag,
                id_project = projetProjetTagDto.id_project
            }).ToList();
            var projetProjetTags = await _projetProjetTagService.CreateBulkProjetProjetTag(projetProjetTagsDtoFull);
            return Ok(projetProjetTags);
        }

        [HttpDelete("{id_project}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteProjetProjetTag([FromRoute] int id_project_tag, [FromRoute] int id_project)
        {
            await _projetProjetTagService.DeleteProjetProjetTag(id_project, id_project_tag);
            return NoContent();
        }

        [HttpDelete("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkProjectProjectTagDto>> DeleteBulkProjetProjetTag([FromRoute] int id_project_tag, [FromBody] List<int> id_projets)
        {
            var projetProjetTagsDtoFull = id_projets.Select(id_project => new CreateProjectProjectTagDto
            {
                id_project_tag = id_project_tag,
                id_project = id_project
            }).ToList();
            var projetProjetTags = await _projetProjetTagService.DeleteBulkProjetProjetTag(projetProjetTagsDtoFull);
            return Ok(projetProjetTags);
        }
    }
}