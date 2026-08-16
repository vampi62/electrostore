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
        private readonly IProjectItemService _projetItemService;

        public ItemProjectController(IProjectItemService projetItemService)
        {
            _projetItemService = projetItemService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedProjectItemDto>>> GetProjetItemsByItemId([FromRoute] int id_item, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'project', 'item'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL filter. Example: 'quantity_project_item=gt=5'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string. Example: 'quantity_project_item,asc' or 'quantity_project_item,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var projetItems = await _projetItemService.GetProjetItemsByItemId(id_item, limit, offset, rsqlDto, sortDto, expand);
            return Ok(projetItems);
        }

        [HttpGet("{id_project}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedProjectItemDto>> GetProjetItemById([FromRoute] int id_item, [FromRoute] int id_project,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'project', 'item'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projetItem = await _projetItemService.GetProjetItemById(id_project, id_item, expand);
            return Ok(projetItem);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjectItemDto>> CreateProjetItem([FromRoute] int id_item, [FromBody] CreateProjectItemByItemDto projetItemDto)
        {
            var projetItemDtoFull = new CreateProjectItemDto
            {
                id_item = id_item,
                id_project = projetItemDto.id_project,
                quantity_project_item = projetItemDto.quantity_project_item
            };
            var projetItem = await _projetItemService.CreateProjetItem(projetItemDtoFull);
            return CreatedAtAction(nameof(GetProjetItemById), new { id_item = projetItem.id_item, id_project = projetItem.id_project }, projetItem);
        }

        [HttpPost("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkProjectItemDto>> CreateBulkProjetItem([FromRoute] int id_item, [FromBody] List<CreateProjectItemByItemDto> projetItemDto)
        {
            var projetItemDtoFull = projetItemDto.Select(x => new CreateProjectItemDto
            {
                id_item = id_item,
                id_project = x.id_project,
                quantity_project_item = x.quantity_project_item
            }).ToList();
            var projetItem = await _projetItemService.CreateBulkProjetItem(projetItemDtoFull);
            return Ok(projetItem);
        }

        [HttpPut("{id_project}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjectItemDto>> UpdateProjetItem([FromRoute] int id_item, [FromRoute] int id_project, [FromBody] UpdateProjectItemDto projetItemDto)
        {
            var projetItem = await _projetItemService.UpdateProjetItem(id_project, id_item, projetItemDto);
            return Ok(projetItem);
        }

        [HttpDelete("{id_project}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteProjetItem([FromRoute] int id_item, [FromRoute] int id_project)
        {
            await _projetItemService.DeleteProjetItem(id_project, id_item);
            return NoContent();
        }
    }
}