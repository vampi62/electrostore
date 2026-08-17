using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.ProjectItemService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/item/{id_item}/project")]

    public class ItemProjectController : ControllerBase
    {
        private readonly IProjectItemService _projectItemService;

        public ItemProjectController(IProjectItemService projectItemService)
        {
            _projectItemService = projectItemService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedProjectItemDto>>> GetProjectItemsByItemId([FromRoute] int id_item, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'project', 'item'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL filter. Example: 'quantity_project_item=gt=5'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string. Example: 'quantity_project_item,asc' or 'quantity_project_item,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var projectItems = await _projectItemService.GetProjectItemsByItemId(id_item, limit, offset, rsqlDto, sortDto, expand);
            return Ok(projectItems);
        }

        [HttpGet("{id_project}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedProjectItemDto>> GetProjectItemById([FromRoute] int id_item, [FromRoute] int id_project,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'project', 'item'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projectItem = await _projectItemService.GetProjectItemById(id_project, id_item, expand);
            return Ok(projectItem);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjectItemDto>> CreateProjectItem([FromRoute] int id_item, [FromBody] CreateProjectItemByItemDto projectItemDto)
        {
            var projectItemDtoFull = new CreateProjectItemDto
            {
                id_item = id_item,
                id_project = projectItemDto.id_project,
                quantity_project_item = projectItemDto.quantity_project_item
            };
            var projectItem = await _projectItemService.CreateProjectItem(projectItemDtoFull);
            return CreatedAtAction(nameof(GetProjectItemById), new { id_item = projectItem.id_item, id_project = projectItem.id_project }, projectItem);
        }

        [HttpPost("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkProjectItemDto>> CreateBulkProjectItem([FromRoute] int id_item, [FromBody] List<CreateProjectItemByItemDto> projectItemDto)
        {
            var projectItemDtoFull = projectItemDto.Select(x => new CreateProjectItemDto
            {
                id_item = id_item,
                id_project = x.id_project,
                quantity_project_item = x.quantity_project_item
            }).ToList();
            var projectItem = await _projectItemService.CreateBulkProjectItem(projectItemDtoFull);
            return Ok(projectItem);
        }

        [HttpPut("{id_project}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjectItemDto>> UpdateProjectItem([FromRoute] int id_item, [FromRoute] int id_project, [FromBody] UpdateProjectItemDto projectItemDto)
        {
            var projectItem = await _projectItemService.UpdateProjectItem(id_project, id_item, projectItemDto);
            return Ok(projectItem);
        }

        [HttpDelete("{id_project}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteProjectItem([FromRoute] int id_item, [FromRoute] int id_project)
        {
            await _projectItemService.DeleteProjectItem(id_project, id_item);
            return NoContent();
        }
    }
}