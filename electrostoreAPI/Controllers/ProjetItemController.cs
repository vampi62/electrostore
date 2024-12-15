using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ProjetItemService;
using Swashbuckle.AspNetCore.Annotations;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/projet/{id_projet}/item")]

    public class ProjetItemController : ControllerBase
    {
        private readonly IProjetItemService _projetItemService;

        public ProjetItemController(IProjetItemService projetItemService)
        {
            _projetItemService = projetItemService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadExtendedProjetItemDto>>> GetProjetItemsByProjetId([FromRoute] int id_projet, [FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery, SwaggerParameter(Description = "Fields to expand. Possible values: 'projet', 'item'. Multiple values can be specified by separating them with ','. Default: \"\"")] string expand = "")
        {
            var projetItems = await _projetItemService.GetProjetItemsByProjetId(id_projet, limit, offset, expand.Split(',').ToList());
            var CountList = await _projetItemService.GetProjetItemsCountByProjetId(id_projet);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers","X-Total-Count");
            return Ok(projetItems);
        }

        [HttpGet("{id_item}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedProjetItemDto>> GetProjetItemById([FromRoute] int id_projet, [FromRoute] int id_item, [FromQuery, SwaggerParameter(Description = "Fields to expand. Possible values: 'projet', 'item'. Multiple values can be specified by separating them with ','. Default: \"\"")] string expand = "")
        {
            var projetItem = await _projetItemService.GetProjetItemById(id_projet, id_item, expand.Split(',').ToList());
            return Ok(projetItem);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetItemDto>> CreateProjetItem([FromRoute] int id_projet, [FromBody] CreateProjetItemByProjetDto projetItemDto)
        {
            var projetItemDtoFull = new CreateProjetItemDto
            {
                id_projet = id_projet,
                id_item = projetItemDto.id_item,
                qte_projet_item = projetItemDto.qte_projet_item
            };
            var projetItem = await _projetItemService.CreateProjetItem(projetItemDtoFull);
            return CreatedAtAction(nameof(GetProjetItemById), new { id_projet = projetItem.id_projet, id_item = projetItem.id_item }, projetItem);
        }

        [HttpPost("bulk")]
		[Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkProjetItemDto>> CreateBulkProjetItem([FromRoute] int id_projet, [FromBody] List<CreateProjetItemByProjetDto> projetItemDto)
        {
            var projetItemDtoFull = projetItemDto.Select(x => new CreateProjetItemDto
            {
                id_projet = id_projet,
                id_item = x.id_item,
                qte_projet_item = x.qte_projet_item
            }).ToList();
            var projetItem = await _projetItemService.CreateBulkProjetItem(projetItemDtoFull);
            return Ok(projetItem);
        }

        [HttpPut("{id_item}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetItemDto>> UpdateProjetItem([FromRoute] int id_projet, [FromRoute] int id_item, [FromBody] UpdateProjetItemDto projetItemDto)
        {
            var projetItem = await _projetItemService.UpdateProjetItem(id_projet, id_item, projetItemDto);
            return Ok(projetItem);
        }

        [HttpDelete("{id_item}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteProjetItem([FromRoute] int id_projet, [FromRoute] int id_item)
        {
            await _projetItemService.DeleteProjetItem(id_projet, id_item);
            return NoContent();
        }
    }
}