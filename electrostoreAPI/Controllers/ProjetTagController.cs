using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.ProjetTagService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
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
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedProjetTagDto>>> GetProjetTags([FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'project_tags'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to select list of ID to research in the base. Multiple values can be specified by separating them with ','.")] List<int>? idResearch = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL string to filter results. Example: 'name_project_tag=like=example'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string to order results. Example: 'id_project_tag,asc' or 'id_project_tag,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var projetTags = await _projetTagService.GetProjetTags(limit, offset, rsqlDto, sortDto, expand, idResearch);
            return Ok(projetTags);
        }

        [HttpGet("{id_project_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedTagDto>> GetProjetTagById([FromRoute] int id_project_tag,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'project_tags'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projetTags = await _projetTagService.GetProjetTagById(id_project_tag, expand);
            return Ok(projetTags);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadTagDto>> CreateProjetTag([FromBody] CreateProjetTagDto projetTag)
        {
            var newProjetTag = await _projetTagService.CreateProjetTag(projetTag);
            return CreatedAtAction(nameof(GetProjetTagById), new { id_project_tag = newProjetTag.id_project_tag }, newProjetTag);
        }
        
        [HttpPost("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkTagDto>> CreateBulkProjetTag([FromBody] List<CreateProjetTagDto> projetTag)
        {
            var newProjetTag = await _projetTagService.CreateBulkProjetTag(projetTag);
            return Ok(newProjetTag);
        }

        [HttpPut("{id_project_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadTagDto>> UpdateProjetTag([FromRoute] int id_project_tag, [FromBody] UpdateProjetTagDto projetTag)
        {
            var tagToUpdate = await _projetTagService.UpdateProjetTag(id_project_tag, projetTag);
            return Ok(tagToUpdate);
        }
        
        [HttpDelete("{id_project_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteProjetTag([FromRoute] int id_project_tag)
        {
            await _projetTagService.DeleteProjetTag(id_project_tag);
            return NoContent();
        }
    }
}