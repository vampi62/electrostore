using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ProjetTagService;
using Swashbuckle.AspNetCore.Annotations;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/projet-tag")]

    public class ProjetTagController : ControllerBase
    {
        private readonly IProjetTagService _projetTagService;

        public ProjetTagController(IProjetTagService projetTagService)
        {
            _projetTagService = projetTagService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadExtendedProjetTagDto>>> GetProjetTags([FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projets_projet_tags'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to select list of ID to research in the base. Multiple values can be specified by separating them with ','.")] List<int>? idResearch = null)
        {
            var projetTags = await _projetTagService.GetProjetTags(limit, offset, expand, idResearch);
            var CountList = await _projetTagService.GetProjetTagsCount();
            Response.Headers["X-Total-Count"] = CountList.ToString();
            Response.Headers.AccessControlExposeHeaders = "X-Total-Count";
            return Ok(projetTags);
        }

        [HttpGet("{id_projet_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedTagDto>> GetProjetTagById([FromRoute] int id_projet_tag, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projets_projet_tags'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projetTags = await _projetTagService.GetProjetTagById(id_projet_tag, expand);
            return Ok(projetTags);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadTagDto>> CreateProjetTag([FromBody] CreateProjetTagDto projetTag)
        {
            var newProjetTag = await _projetTagService.CreateProjetTag(projetTag);
            return CreatedAtAction(nameof(GetProjetTagById), new { id_projet_tag = newProjetTag.id_projet_tag }, newProjetTag);
        }
        
        [HttpPost("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkTagDto>> CreateBulkProjetTag([FromBody] List<CreateProjetTagDto> projetTag)
        {
            var newProjetTag = await _projetTagService.CreateBulkProjetTag(projetTag);
            return Ok(newProjetTag);
        }

        [HttpPut("{id_projet_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadTagDto>> UpdateProjetTag([FromRoute] int id_projet_tag, [FromBody] UpdateProjetTagDto projetTag)
        {
            var tagToUpdate = await _projetTagService.UpdateProjetTag(id_projet_tag, projetTag);
            return Ok(tagToUpdate);
        }
        
        [HttpDelete("{id_projet_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteProjetTag([FromRoute] int id_projet_tag)
        {
            await _projetTagService.DeleteProjetTag(id_projet_tag);
            return NoContent();
        }
    }
}