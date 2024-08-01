using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.StoreTagService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/store/{id_store}/tag")]

    public class StoreTagController : ControllerBase
    {
        private readonly IStoreTagService _storeTagService;

        public StoreTagController(IStoreTagService storeTagService)
        {
            _storeTagService = storeTagService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReadStoreTagDto>>> GetStoresTagsByStoreId([FromRoute] int id_store, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var storeTags = await _storeTagService.GetStoresTagsByStoreId(id_store, limit, offset);
            return Ok(storeTags);
        }
        
        [HttpGet("{id_tag}")]
        public async Task<ActionResult<ReadStoreTagDto>> GetStoreTagById([FromRoute] int id_store, [FromRoute] int id_tag)
        {
            var storeTag = await _storeTagService.GetStoreTagById(id_store, id_tag);
            return Ok(storeTag);
        }

        [HttpPost("{id_tag}")]
        public async Task<ActionResult<ReadStoreTagDto>> CreateStoreTag([FromRoute] int id_store, [FromRoute] int id_tag)
        {
            var storeTagDto = new CreateStoreTagDto
            {
                id_store = id_store,
                id_tag = id_tag
            };
            var newStoreTag = await _storeTagService.CreateStoreTag(storeTagDto);
            return CreatedAtAction(nameof(GetStoreTagById), new { id_store = newStoreTag.id_store, id_tag = newStoreTag.id_tag }, newStoreTag);
        }

        [HttpDelete("{id_tag}")]
        public async Task<ActionResult> DeleteStoreTag([FromRoute] int id_store, [FromRoute] int id_tag)
        {
            await _storeTagService.DeleteStoreTag(id_store, id_tag);
            return NoContent();
        }
    }
}