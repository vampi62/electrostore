using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ProjetItemService;
using Swashbuckle.AspNetCore.Annotations;
using electrostore.Extensions;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/item/{id_item}/projet")]

    public class ItemProjetController : ControllerBase
    {
        private readonly IProjetItemService _projetItemService;

        public ItemProjetController(IProjetItemService projetItemService)
        {
            _projetItemService = projetItemService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadExtendedProjetItemDto>>> GetProjetItemsByItemId([FromRoute] int id_item, [FromQuery] int limit = 100, [FromQuery] int offset = 0,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projet', 'item'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) RSQL filter. Example: 'qte_projet_item=gt=5'.")] string? filter = null,
        [FromQuery, SwaggerParameter(Description = "(Optional) Sort string. Example: 'qte_projet_item,asc' or 'qte_projet_item,desc'.")] string? sort = null)
        {
            var rsqlDto = ParserExtensions.ParseFilter(filter ?? string.Empty);
            var sortDto = ParserExtensions.ParseSort(sort ?? string.Empty);
            var projetItems = await _projetItemService.GetProjetItemsByItemId(id_item, limit, offset, rsqlDto, sortDto, expand);
            return Ok(projetItems);
        }

        [HttpGet("{id_projet}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedProjetItemDto>> GetProjetItemById([FromRoute] int id_item, [FromRoute] int id_projet,
        [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'projet', 'item'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var projetItem = await _projetItemService.GetProjetItemById(id_projet, id_item, expand);
            return Ok(projetItem);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetItemDto>> CreateProjetItem([FromRoute] int id_item, [FromBody] CreateProjetItemByItemDto projetItemDto)
        {
            var projetItemDtoFull = new CreateProjetItemDto
            {
                id_item = id_item,
                id_projet = projetItemDto.id_projet,
                qte_projet_item = projetItemDto.qte_projet_item
            };
            var projetItem = await _projetItemService.CreateProjetItem(projetItemDtoFull);
            return CreatedAtAction(nameof(GetProjetItemById), new { id_item = projetItem.id_item, id_projet = projetItem.id_projet }, projetItem);
        }

        [HttpPost("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkProjetItemDto>> CreateBulkProjetItem([FromRoute] int id_item, [FromBody] List<CreateProjetItemByItemDto> projetItemDto)
        {
            var projetItemDtoFull = projetItemDto.Select(x => new CreateProjetItemDto
            {
                id_item = id_item,
                id_projet = x.id_projet,
                qte_projet_item = x.qte_projet_item
            }).ToList();
            var projetItem = await _projetItemService.CreateBulkProjetItem(projetItemDtoFull);
            return Ok(projetItem);
        }

        [HttpPut("{id_projet}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetItemDto>> UpdateProjetItem([FromRoute] int id_item, [FromRoute] int id_projet, [FromBody] UpdateProjetItemDto projetItemDto)
        {
            var projetItem = await _projetItemService.UpdateProjetItem(id_projet, id_item, projetItemDto);
            return Ok(projetItem);
        }

        [HttpDelete("{id_projet}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteProjetItem([FromRoute] int id_item, [FromRoute] int id_projet)
        {
            await _projetItemService.DeleteProjetItem(id_projet, id_item);
            return NoContent();
        }
    }
}