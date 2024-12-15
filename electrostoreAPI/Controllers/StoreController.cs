using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.StoreService;
using Swashbuckle.AspNetCore.Annotations;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/store")]
    public class StoresController : ControllerBase
    {
        private readonly IStoreService _storeService;

        public StoresController(IStoreService storeService)
        {
            _storeService = storeService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadExtendedStoreDto>>> GetStores([FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery, SwaggerParameter(Description = "Fields to expand. Possible values: 'boxs', 'leds', 'stores_tags'. Multiple values can be specified by separating them with ','. Default: \"\"")] string expand = "", [FromQuery] string idResearch = "")
        {
            var idList = string.IsNullOrWhiteSpace(idResearch) ? null : idResearch.Split(',').Where(id => int.TryParse(id, out _)).Select(int.Parse).ToList();
            var stores = await _storeService.GetStores(limit, offset, expand.Split(',').ToList(), idList);
            var CountList = await _storeService.GetStoresCount();
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers","X-Total-Count");
            return Ok(stores);
        }

        [HttpGet("{id_store}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedStoreDto>> GetStoreById([FromRoute] int id_store, [FromQuery, SwaggerParameter(Description = "Fields to expand. Possible values: 'boxs', 'leds', 'stores_tags'. Multiple values can be specified by separating them with ','. Default: \"\"")] string expand = "")
        {
            var store = await _storeService.GetStoreById(id_store, expand.Split(',').ToList());
            return Ok(store);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadStoreDto>> CreateStore([FromBody] CreateStoreDto store)
        {
            var newStore = await _storeService.CreateStore(store);
            return CreatedAtAction(nameof(GetStoreById), new { id_store = newStore.id_store }, newStore);
        }

        [HttpPut("{id_store}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadStoreDto>> UpdateStore([FromRoute] int id_store, [FromBody] UpdateStoreDto store)
        {
            var storeToUpdate = await _storeService.UpdateStore(id_store, store);
            return Ok(storeToUpdate);
        }

        [HttpDelete("{id_store}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteStore([FromRoute] int id_store)
        {
            await _storeService.DeleteStore(id_store);
            return NoContent();
        }
    }
}