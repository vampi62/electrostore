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
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadProjetItemDto>>> GetProjetItemsByItemId([FromRoute] int id_item, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var projetItems = await _projetItemService.GetProjetItemsByItemId(id_item, limit, offset);
            var CountList = await _projetItemService.GetProjetItemsCountByItemId(id_item);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");
            return Ok(projetItems);
        }

        [HttpGet("{id_projet}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetItemDto>> GetProjetItemById([FromRoute] int id_item, [FromRoute] int id_projet)
        {
            var projetItem = await _projetItemService.GetProjetItemById(id_projet, id_item);
            return Ok(projetItem);
        }

        [HttpPost("{id_projet}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadProjetItemDto>> CreateProjetItem([FromRoute] int id_item, [FromRoute] int id_projet, [FromBody] CreateProjetItemByItemDto projetItemDto)
        {
            var projetItemDtoFull = new CreateProjetItemDto
            {
                id_item = id_item,
                id_projet = id_projet,
                qte_projet_item = projetItemDto.qte_projet_item
            };
            var projetItem = await _projetItemService.CreateProjetItem(projetItemDtoFull);
            return CreatedAtAction(nameof(GetProjetItemById), new { id_item = projetItem.id_item, id_projet = projetItem.id_projet }, projetItem);
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