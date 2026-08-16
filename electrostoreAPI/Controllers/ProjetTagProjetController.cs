using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.ProjetProjetTagService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/projet-tag/{id_project_tag}/projet")]

    public class ProjetTagProjetController : ControllerBase
    {
        private readonly IProjetProjetTagService _projetProjetTagService;

        public ProjetTagProjetController(IProjetProjetTagService projetProjetTagService)
        {
            _projetProjetTagService = projetProjetTagService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedProjetProjetTagDto>>> GetProjetsProjetTagsByprojetTagId([FromRoute] int id_project_tag, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projet_tag', 'projet'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
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
        public async Task<ActionResult<ReadExtendedProjetProjetTagDto>> GetProjetProjetTagById([FromRoute] int id_project_tag, [FromRoute] int id_project,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projet_tag', 'projet'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projetProjetTag = await _projetProjetTagService.GetProjetProjetTagById(id_project, id_project_tag, expand);
            return Ok(projetProjetTag);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetProjetTagDto>> CreateProjetProjetTag([FromRoute] int id_project_tag, [FromBody] CreateProjetProjetTagByProjetTagDto projetProjetTagDto)
        {
            var projetProjetTagFull = new CreateProjetProjetTagDto
            {
                id_project_tag = id_project_tag,
                id_project = projetProjetTagDto.id_project
            };
            var newProjetProjetTag = await _projetProjetTagService.CreateProjetProjetTag(projetProjetTagFull);
            return CreatedAtAction(nameof(GetProjetProjetTagById), new { id_project_tag = newProjetProjetTag.id_project_tag, id_project = newProjetProjetTag.id_project }, newProjetProjetTag);
        }

        [HttpPost("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkProjetProjetTagDto>> CreateBulkProjetProjetTag([FromRoute] int id_project_tag, [FromBody] List<CreateProjetProjetTagByProjetTagDto> projetProjetTagsDto)
        {
            var projetProjetTagsDtoFull = projetProjetTagsDto.Select(projetProjetTagDto => new CreateProjetProjetTagDto
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
        public async Task<ActionResult<ReadBulkProjetProjetTagDto>> DeleteBulkProjetProjetTag([FromRoute] int id_project_tag, [FromBody] List<int> id_projets)
        {
            var projetProjetTagsDtoFull = id_projets.Select(id_project => new CreateProjetProjetTagDto
            {
                id_project_tag = id_project_tag,
                id_project = id_project
            }).ToList();
            var projetProjetTags = await _projetProjetTagService.DeleteBulkProjetProjetTag(projetProjetTagsDtoFull);
            return Ok(projetProjetTags);
        }
    }
}