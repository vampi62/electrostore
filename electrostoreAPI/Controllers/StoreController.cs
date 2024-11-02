using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.StoreService;

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
        public async Task<ActionResult<IEnumerable<ReadStoreDto>>> GetStores([FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var stores = await _storeService.GetStores(limit, offset);
            return Ok(stores);
        }

        [HttpGet("{id_store}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadStoreDto>> GetStoreById([FromRoute] int id_store)
        {
            var store = await _storeService.GetStoreById(id_store);
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