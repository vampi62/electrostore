using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ProjetProjetTagService;
using Swashbuckle.AspNetCore.Annotations;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/projet-tag/{id_projet_tag}/projet")]

    public class ProjetTagProjetController : ControllerBase
    {
        private readonly IProjetProjetTagService _projetProjetTagService;

        public ProjetTagProjetController(IProjetProjetTagService projetProjetTagService)
        {
            _projetProjetTagService = projetProjetTagService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadExtendedProjetProjetTagDto>>> GetProjetsProjetTagsByprojetTagId([FromRoute] int id_projet_tag, [FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projet_tag', 'projet'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projetProjetTags = await _projetProjetTagService.GetProjetsProjetTagsByprojetTagId(id_projet_tag, limit, offset, expand);
            var CountList = await _projetProjetTagService.GetProjetsProjetTagsCountByprojetTagId(id_projet_tag);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers","X-Total-Count");
            return Ok(projetProjetTags);
        }

        [HttpGet("{id_projet}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedProjetProjetTagDto>> GetProjetProjetTagById([FromRoute] int id_projet_tag, [FromRoute] int id_projet, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projet_tag', 'projet'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projetProjetTag = await _projetProjetTagService.GetProjetProjetTagById(id_projet, id_projet_tag, expand);
            return Ok(projetProjetTag);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetProjetTagDto>> CreateProjetProjetTag([FromRoute] int id_projet_tag, [FromBody] CreateProjetProjetTagByProjetTagDto projetProjetTagDto)
        {
            var projetProjetTagFull = new CreateProjetProjetTagDto
            {
                id_projet_tag = id_projet_tag,
                id_projet = projetProjetTagDto.id_projet
            };
            var newProjetProjetTag = await _projetProjetTagService.CreateProjetProjetTag(projetProjetTagFull);
            return CreatedAtAction(nameof(GetProjetProjetTagById), new { id_projet_tag = newProjetProjetTag.id_projet_tag, id_projet = newProjetProjetTag.id_projet }, newProjetProjetTag);
        }

        [HttpPost("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkProjetProjetTagDto>> CreateBulkProjetProjetTag([FromRoute] int id_projet_tag, [FromBody] List<CreateProjetProjetTagByProjetTagDto> projetProjetTagsDto)
        {
            var projetProjetTagsDtoFull = projetProjetTagsDto.Select(projetProjetTagDto => new CreateProjetProjetTagDto
            {
                id_projet_tag = id_projet_tag,
                id_projet = projetProjetTagDto.id_projet
            }).ToList();
            var projetProjetTags = await _projetProjetTagService.CreateBulkProjetProjetTag(projetProjetTagsDtoFull);
            return Ok(projetProjetTags);
        }

        [HttpDelete("{id_projet}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteProjetProjetTag([FromRoute] int id_projet_tag, [FromRoute] int id_projet)
        {
            await _projetProjetTagService.DeleteProjetProjetTag(id_projet, id_projet_tag);
            return NoContent();
        }

        [HttpDelete("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkProjetProjetTagDto>> DeleteBulkProjetProjetTag([FromRoute] int id_projet_tag, [FromBody] List<int> id_projets)
        {
            var projetProjetTagsDtoFull = id_projets.Select(id_projet => new CreateProjetProjetTagDto
            {
                id_projet_tag = id_projet_tag,
                id_projet = id_projet
            }).ToList();
            var projetProjetTags = await _projetProjetTagService.DeleteBulkProjetProjetTag(projetProjetTagsDtoFull);
            return Ok(projetProjetTags);
        }
    }
}