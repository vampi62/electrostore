using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadProjetItemDto>>> GetProjetItemsByProjetId([FromRoute] int id_projet)
        {
            var projetItems = await _projetItemService.GetProjetItemsByProjetId(id_projet);
            return Ok(projetItems);
        }

        [HttpGet("{id_item}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetItemDto>> GetProjetItemById([FromRoute] int id_projet, [FromRoute] int id_item)
        {
            var projetItem = await _projetItemService.GetProjetItemById(id_projet, id_item);
            return Ok(projetItem);
        }

        [HttpPost("{id_item}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetItemDto>> AddProjetItem([FromRoute] int id_projet, [FromRoute] int id_item, [FromBody] CreateProjetItemByProjetDto projetItemDto)
        {
            var projetItemDtoFull = new CreateProjetItemDto
            {
                id_projet = id_projet,
                id_item = id_item,
                qte_projetitem = projetItemDto.qte_projetitem
            };
            var projetItem = await _projetItemService.CreateProjetItem(projetItemDtoFull);
            return CreatedAtAction(nameof(GetProjetItemById), new { id_projet = projetItem.id_projet, id_item = projetItem.id_item }, projetItem);
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