using Microsoft.AspNetCore.Mvc;
using electrostore.Dto;
using electrostore.Services.ProjetItemService;

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
        public async Task<ActionResult<IEnumerable<ReadProjetItemDto>>> GetProjetItemsByProjetId([FromRoute] int id_projet)
        {
            var projetItems = await _projetItemService.GetProjetItemsByProjetId(id_projet);
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

        [HttpGet("{id_item}")]
        public async Task<ActionResult<ReadProjetItemDto>> GetProjetItemById([FromRoute] int id_projet, [FromRoute] int id_item)
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
        public async Task<ActionResult<ReadProjetItemDto>> AddProjetItem([FromRoute] int id_projet, [FromBody] CreateProjetItemByProjetDto projetItemDto)
        {
            var projetItemDtoFull = new CreateProjetItemDto
            {
                id_projet = id_projet,
                id_item = projetItemDto.id_item,
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
            return CreatedAtAction(nameof(GetProjetItemById), new { id_projet = projetItem.Value.id_projet, id_item = projetItem.Value.id_item }, projetItem.Value);
        }

        [HttpPut("{id_item}")]
        public async Task<ActionResult<ReadProjetItemDto>> UpdateProjetItem([FromRoute] int id_projet, [FromRoute] int id_item, [FromBody] UpdateProjetItemDto projetItemDto)
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

        [HttpDelete("{id_item}")]
        public async Task<ActionResult> DeleteProjetItem([FromRoute] int id_projet, [FromRoute] int id_item)
        {
            await _projetItemService.DeleteProjetItem(id_projet, id_item);
            return NoContent();
        }
    }
}