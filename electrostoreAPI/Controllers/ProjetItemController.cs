using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Services.ProjetItemService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/projet/{id_project}/item")]

    public class ProjetItemController : ControllerBase
    {
        private readonly IProjetItemService _projetItemService;

        public ProjetItemController(IProjetItemService projetItemService)
        {
            _projetItemService = projetItemService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedProjetItemDto>>> GetProjetItemsByProjetId([FromRoute] int id_project, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projet', 'item'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
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
        public async Task<ActionResult<ReadExtendedProjetItemDto>> GetProjetItemById([FromRoute] int id_project, [FromRoute] int id_item,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projet', 'item'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projetItem = await _projetItemService.GetProjetItemById(id_project, id_item, expand);
            return Ok(projetItem);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetItemDto>> CreateProjetItem([FromRoute] int id_project, [FromBody] CreateProjetItemByProjetDto projetItemDto)
        {
            var projetItemDtoFull = new CreateProjetItemDto
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
        public async Task<ActionResult<ReadBulkProjetItemDto>> CreateBulkProjetItem([FromRoute] int id_project, [FromBody] List<CreateProjetItemByProjetDto> projetItemDto)
        {
            var projetItemDtoFull = projetItemDto.Select(x => new CreateProjetItemDto
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
        public async Task<ActionResult<ReadProjetItemDto>> UpdateProjetItem([FromRoute] int id_project, [FromRoute] int id_item, [FromBody] UpdateProjetItemDto projetItemDto)
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