using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.ProjetProjetTagService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/projet/{id_project}/projet-tag")]

    public class ProjetProjetTagController : ControllerBase
    {
        private readonly IProjetProjetTagService _projetProjetTagService;

        public ProjetProjetTagController(IProjetProjetTagService projetProjetTagService)
        {
            _projetProjetTagService = projetProjetTagService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedProjetProjetTagDto>>> GetProjetsProjetTagsByProjetId([FromRoute] int id_project, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projet_tag', 'projet'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL filter. Example: 'id_project_tag==5'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string. Example: 'id_project_tag,asc' or 'id_project_tag,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var projetProjetTags = await _projetProjetTagService.GetProjetsProjetTagsByProjetId(id_project, limit, offset, rsqlDto, sortDto, expand);
            return Ok(projetProjetTags);
        }
        
        [HttpGet("{id_project_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedProjetProjetTagDto>> GetProjetProjetTagById([FromRoute] int id_project, [FromRoute] int id_project_tag,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projet_tag', 'projet'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projetProjetTag = await _projetProjetTagService.GetProjetProjetTagById(id_project, id_project_tag, expand);
            return Ok(projetProjetTag);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetProjetTagDto>> CreateProjetProjetTag([FromRoute] int id_project, [FromBody] CreateProjetProjetTagByProjetDto projetProjetTagDto)
        {
            var projetProjetTagFull = new CreateProjetProjetTagDto
            {
                id_project = id_project,
                id_project_tag = projetProjetTagDto.id_project_tag
            };
            var newProjetProjetTag = await _projetProjetTagService.CreateProjetProjetTag(projetProjetTagFull);
            return CreatedAtAction(nameof(GetProjetProjetTagById), new { id_project = newProjetProjetTag.id_project, id_project_tag = newProjetProjetTag.id_project_tag }, newProjetProjetTag);
        }

        [HttpPost("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkProjetProjetTagDto>> CreateBulkProjetProjetTag([FromRoute] int id_project, [FromBody] List<CreateProjetProjetTagByProjetDto> projetProjetTagsDto)
        {
            var projetProjetTagsDtoFull = projetProjetTagsDto.Select(projetProjetTagDto => new CreateProjetProjetTagDto
            {
                id_project = id_project,
                id_project_tag = projetProjetTagDto.id_project_tag
            }).ToList();
            var projetProjetTags = await _projetProjetTagService.CreateBulkProjetProjetTag(projetProjetTagsDtoFull);
            return Ok(projetProjetTags);
        }

        [HttpDelete("{id_project_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteProjetProjetTag([FromRoute] int id_project, [FromRoute] int id_project_tag)
        {
            await _projetProjetTagService.DeleteProjetProjetTag(id_project, id_project_tag);
            return NoContent();
        }

        [HttpDelete("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkProjetProjetTagDto>> DeleteBulkProjetProjetTag([FromRoute] int id_project, [FromBody] List<int> id_projet_tags)
        {
            var projetProjetTagsDtoFull = id_projet_tags.Select(id_project_tag => new CreateProjetProjetTagDto
            {
                id_project = id_project,
                id_project_tag = id_project_tag
            }).ToList();
            var projetProjetTags = await _projetProjetTagService.DeleteBulkProjetProjetTag(projetProjetTagsDtoFull);
            return Ok(projetProjetTags);
        }
    }
}