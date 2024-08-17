using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ProjetItemService;

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
        public async Task<ActionResult<IEnumerable<ReadProjetItemDto>>> GetProjetItemsByItemId([FromRoute] int id_item, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var projetItems = await _projetItemService.GetProjetItemsByItemId(id_item, limit, offset);
            if (projetItems.Result is BadRequestObjectResult)
            {
                return projetItems.Result;
            }
            if (projetItems.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(projetItems.Value);
        }

        [HttpGet("{id_projet}")]
        public async Task<ActionResult<ReadProjetItemDto>> GetProjetItemById([FromRoute] int id_item, [FromRoute] int id_projet)
        {
            var projetItem = await _projetItemService.GetProjetItemById(id_projet, id_item);
            if (projetItem.Result is BadRequestObjectResult)
            {
                return projetItem.Result;
            }
            if (projetItem.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(projetItem.Value);
        }

        [HttpPost]
        public async Task<ActionResult<ReadProjetItemDto>> CreateProjetItem([FromRoute] int id_item, [FromBody] CreateProjetItemByItemDto projetItemDto)
        {
            var projetItemDtoFull = new CreateProjetItemDto
            {
                id_item = id_item,
                id_projet = projetItemDto.id_projet,
                qte_projetitem = projetItemDto.qte_projetitem
            };
            var projetItem = await _projetItemService.CreateProjetItem(projetItemDtoFull);
            if (projetItem.Result is BadRequestObjectResult)
            {
                return projetItem.Result;
            }
            if (projetItem.Value == null)
            {
                return StatusCode(500);
            }
            return CreatedAtAction(nameof(GetProjetItemById), new { id_item = projetItem.Value.id_item, id_projet = projetItem.Value.id_projet }, projetItem.Value);
        }

        [HttpPut("{id_projet}")]
        public async Task<ActionResult<ReadProjetItemDto>> UpdateProjetItem([FromRoute] int id_item, [FromRoute] int id_projet, [FromBody] UpdateProjetItemDto projetItemDto)
        {
            var projetItem = await _projetItemService.UpdateProjetItem(id_projet, id_item, projetItemDto);
            if (projetItem.Result is BadRequestObjectResult)
            {
                return projetItem.Result;
            }
            if (projetItem.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(projetItem.Value);
        }

        [HttpDelete("{id_projet}")]
        public async Task<ActionResult> DeleteProjetItem([FromRoute] int id_item, [FromRoute] int id_projet)
        {
            await _projetItemService.DeleteProjetItem(id_projet, id_item);
            return NoContent();
        }
    }
}