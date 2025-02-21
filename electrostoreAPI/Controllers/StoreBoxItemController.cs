using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.ItemBoxService;
using Swashbuckle.AspNetCore.Annotations;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/store/{id_store}/box/{id_box}/item")]

    public class StoreBoxItemController : ControllerBase
    {
        private readonly IItemBoxService _itemBoxService;

        public StoreBoxItemController(IItemBoxService itemBoxService)
        {
            _itemBoxService = itemBoxService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadExtendedItemBoxDto>>> GetItemsBoxsByBoxId([FromRoute] int id_store, [FromRoute] int id_box, [FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'item', 'box'. Multiple values can be specified by separating them with ','.")] string? expand = null)
        {
            await _itemBoxService.CheckIfStoreExists(id_store,id_box);
            var itemsBoxs = await _itemBoxService.GetItemsBoxsByBoxId(id_box, limit, offset, expand?.Split(',').ToList());
            var CountList = await _itemBoxService.GetItemsBoxsCountByBoxId(id_box);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers","X-Total-Count");
            return Ok(itemsBoxs);
        }

        [HttpGet("{id_item}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedItemBoxDto>> GetItemBoxById([FromRoute] int id_store, [FromRoute] int id_box, [FromRoute] int id_item, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'item', 'box'. Multiple values can be specified by separating them with ','.")] string? expand = null)
        {
            await _itemBoxService.CheckIfStoreExists(id_store,id_box);
            var itemBox = await _itemBoxService.GetItemBoxById(id_box, id_item, expand?.Split(',').ToList());
            return Ok(itemBox);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadItemBoxDto>> CreateItemBox([FromRoute] int id_store, [FromRoute] int id_box, [FromBody] CreateItemBoxByBoxDto itemBoxDto)
        {
            await _itemBoxService.CheckIfStoreExists(id_store,id_box);
            var itemBoxDtoFull = new CreateItemBoxDto
            {
                id_box = id_box,
                id_item = itemBoxDto.id_item,
                qte_item_box = itemBoxDto.qte_item_box,
                seuil_max_item_item_box = itemBoxDto.seuil_max_item_item_box
            };
            var itemBox = await _itemBoxService.CreateItemBox(itemBoxDtoFull);
            return CreatedAtAction(nameof(GetItemBoxById), new { id_store = id_store, id_box = itemBox.id_box, id_item = itemBox.id_item }, itemBox);
        }

        [HttpPut("{id_item}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadItemBoxDto>> UpdateItemBox([FromRoute] int id_store, [FromRoute] int id_box, [FromRoute] int id_item, [FromBody] UpdateItemBoxDto itemBoxDto)
        {
            await _itemBoxService.CheckIfStoreExists(id_store,id_box);
            var itemBox = await _itemBoxService.UpdateItemBox(id_box, id_item, itemBoxDto);
            return Ok(itemBox);
        }

        [HttpDelete("{id_item}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteItemBox([FromRoute] int id_store, [FromRoute] int id_box, [FromRoute] int id_item)
        {
            await _itemBoxService.CheckIfStoreExists(id_store,id_box);
            await _itemBoxService.DeleteItemBox(id_box, id_item);
            return NoContent();
        }
    }
}