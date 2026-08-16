using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.ProjectItemService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/project/{id_project}/item")]

    public class ProjectItemController : ControllerBase
    {
        private readonly IProjectItemService _projetItemService;

        public ProjectItemController(IProjectItemService projetItemService)
        {
            _projetItemService = projetItemService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedProjectItemDto>>> GetProjetItemsByProjetId([FromRoute] int id_project, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'project', 'item'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL filter. Example: 'quantity_project_item=gt=5'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string. Example: 'quantity_project_item,asc' or 'quantity_project_item,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var projetItems = await _projetItemService.GetProjetItemsByProjetId(id_project, limit, offset, rsqlDto, sortDto, expand);
            return Ok(projetItems);
        }

        [HttpGet("{id_item}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedProjectItemDto>> GetProjetItemById([FromRoute] int id_project, [FromRoute] int id_item,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'project', 'item'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projetItem = await _projetItemService.GetProjetItemById(id_project, id_item, expand);
            return Ok(projetItem);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjectItemDto>> CreateProjetItem([FromRoute] int id_project, [FromBody] CreateProjectItemByProjectDto projetItemDto)
        {
            var projetItemDtoFull = new CreateProjectItemDto
            {
                id_project = id_project,
                id_item = projetItemDto.id_item,
                quantity_project_item = projetItemDto.quantity_project_item
            };
            var projetItem = await _projetItemService.CreateProjetItem(projetItemDtoFull);
            return CreatedAtAction(nameof(GetProjetItemById), new { id_project = projetItem.id_project, id_item = projetItem.id_item }, projetItem);
        }

        [HttpPost("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkProjectItemDto>> CreateBulkProjetItem([FromRoute] int id_project, [FromBody] List<CreateProjectItemByProjectDto> projetItemDto)
        {
            var projetItemDtoFull = projetItemDto.Select(x => new CreateProjectItemDto
            {
                id_project = id_project,
                id_item = x.id_item,
                quantity_project_item = x.quantity_project_item
            }).ToList();
            var projetItem = await _projetItemService.CreateBulkProjetItem(projetItemDtoFull);
            return Ok(projetItem);
        }

        [HttpPut("{id_item}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjectItemDto>> UpdateProjetItem([FromRoute] int id_project, [FromRoute] int id_item, [FromBody] UpdateProjectItemDto projetItemDto)
        {
            var projetItem = await _projetItemService.UpdateProjetItem(id_project, id_item, projetItemDto);
            return Ok(projetItem);
        }

        [HttpDelete("{id_item}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteProjetItem([FromRoute] int id_project, [FromRoute] int id_item)
        {
            await _projetItemService.DeleteProjetItem(id_project, id_item);
            return NoContent();
        }
    }
}