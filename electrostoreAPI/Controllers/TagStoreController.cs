using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.StoreTagService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/tag/{id_tag}/store")]

    public class TagStoreController : ControllerBase
    {
        private readonly IStoreTagService _storeTagService;

        public TagStoreController(IStoreTagService storeTagService)
        {
            _storeTagService = storeTagService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadStoreTagDto>>> GetStoresTagsByTagId([FromRoute] int id_tag, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var storeTags = await _storeTagService.GetStoresTagsByTagId(id_tag, limit, offset);
            return Ok(storeTags);
        }

        [HttpGet("{id_store}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadStoreTagDto>> GetStoreTagById([FromRoute] int id_tag, [FromRoute] int id_store)
        {
            var storeTag = await _storeTagService.GetStoreTagById(id_store, id_tag);
            return Ok(storeTag);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadStoreTagDto>> CreateStoresTag([FromRoute] int id_tag, [FromBody] int[] stores)
        {
            var storeTags = await _storeTagService.CreateStoreTags(null, id_tag, null, stores);
            return Ok(storeTags);
        }

        [HttpPost("{id_store}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadStoreTagDto>> CreateStoreTag([FromRoute] int id_tag, [FromRoute] int id_store)
        {
            var storeTagDto = new CreateStoreTagDto
            {
                id_tag = id_tag,
                id_store = id_store
            };
            var storeTag = await _storeTagService.CreateStoreTag(storeTagDto);
            return CreatedAtAction(nameof(GetStoreTagById), new { id_tag = storeTag.id_tag, id_store = storeTag.id_store }, storeTag);
        }

        [HttpDelete("{id_store}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteStoreTag([FromRoute] int id_tag, [FromRoute] int id_store)
        {
            await _storeTagService.DeleteStoreTag(id_store, id_tag);
            return NoContent();
        }
    }
}