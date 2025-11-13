using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ProjetProjetTagService;
using Swashbuckle.AspNetCore.Annotations;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/projet/{id_projet}/projet-tag")]

    public class ProjetProjetTagController : ControllerBase
    {
        private readonly IProjetProjetTagService _projetProjetTagService;

        public ProjetProjetTagController(IProjetProjetTagService projetProjetTagService)
        {
            _projetProjetTagService = projetProjetTagService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadExtendedProjetProjetTagDto>>> GetProjetsProjetTagsByProjetId([FromRoute] int id_projet, [FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projet_tag', 'projet'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projetProjetTags = await _projetProjetTagService.GetProjetsProjetTagsByProjetId(id_projet, limit, offset, expand);
            var CountList = await _projetProjetTagService.GetProjetsProjetTagsCountByProjetId(id_projet);
            Response.Headers["X-Total-Count"] = CountList.ToString();
            Response.Headers.AccessControlExposeHeaders = "X-Total-Count";
            return Ok(projetProjetTags);
        }
        
        [HttpGet("{id_projet_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedProjetProjetTagDto>> GetProjetProjetTagById([FromRoute] int id_projet, [FromRoute] int id_projet_tag, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projet_tag', 'projet'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projetProjetTag = await _projetProjetTagService.GetProjetProjetTagById(id_projet, id_projet_tag, expand);
            return Ok(projetProjetTag);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetProjetTagDto>> CreateProjetProjetTag([FromRoute] int id_projet, [FromBody] CreateProjetProjetTagByProjetDto projetProjetTagDto)
        {
            var projetProjetTagFull = new CreateProjetProjetTagDto
            {
                id_projet = id_projet,
                id_projet_tag = projetProjetTagDto.id_projet_tag
            };
            var newProjetProjetTag = await _projetProjetTagService.CreateProjetProjetTag(projetProjetTagFull);
            return CreatedAtAction(nameof(GetProjetProjetTagById), new { id_projet = newProjetProjetTag.id_projet, id_projet_tag = newProjetProjetTag.id_projet_tag }, newProjetProjetTag);
        }

        [HttpPost("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkProjetProjetTagDto>> CreateBulkProjetProjetTag([FromRoute] int id_projet, [FromBody] List<CreateProjetProjetTagByProjetDto> projetProjetTagsDto)
        {
            var projetProjetTagsDtoFull = projetProjetTagsDto.Select(projetProjetTagDto => new CreateProjetProjetTagDto
            {
                id_projet = id_projet,
                id_projet_tag = projetProjetTagDto.id_projet_tag
            }).ToList();
            var projetProjetTags = await _projetProjetTagService.CreateBulkProjetProjetTag(projetProjetTagsDtoFull);
            return Ok(projetProjetTags);
        }

        [HttpDelete("{id_projet_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteProjetProjetTag([FromRoute] int id_projet, [FromRoute] int id_projet_tag)
        {
            await _projetProjetTagService.DeleteProjetProjetTag(id_projet, id_projet_tag);
            return NoContent();
        }

        [HttpDelete("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkProjetProjetTagDto>> DeleteBulkProjetProjetTag([FromRoute] int id_projet, [FromBody] List<int> id_projet_tags)
        {
            var projetProjetTagsDtoFull = id_projet_tags.Select(id_projet_tag => new CreateProjetProjetTagDto
            {
                id_projet = id_projet,
                id_projet_tag = id_projet_tag
            }).ToList();
            var projetProjetTags = await _projetProjetTagService.DeleteBulkProjetProjetTag(projetProjetTagsDtoFull);
            return Ok(projetProjetTags);
        }
    }
}