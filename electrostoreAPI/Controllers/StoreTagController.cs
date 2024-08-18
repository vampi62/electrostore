using Microsoft.AspNetCore.Mvc;
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
            if (storeTags.Result is BadRequestObjectResult)
            {
                return storeTags.Result;
            }
            if (storeTags.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(storeTags.Value);
        }
        
        [HttpGet("{id_tag}")]
        public async Task<ActionResult<ReadStoreTagDto>> GetStoreTagById([FromRoute] int id_store, [FromRoute] int id_tag)
        {
            var storeTag = await _storeTagService.GetStoreTagById(id_store, id_tag);
            if (storeTag.Result is BadRequestObjectResult)
            {
                return storeTag.Result;
            }
            if (storeTag.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(storeTag.Value);
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
            if (newStoreTag.Result is BadRequestObjectResult)
            {
                return newStoreTag.Result;
            }
            if (newStoreTag.Value == null)
            {
                return StatusCode(500);
            }
            return CreatedAtAction(nameof(GetStoreTagById), new { id_store = newStoreTag.Value.id_store, id_tag = newStoreTag.Value.id_tag }, newStoreTag.Value);
        }

        [HttpDelete("{id_tag}")]
        public async Task<ActionResult> DeleteStoreTag([FromRoute] int id_store, [FromRoute] int id_tag)
        {
            await _storeTagService.DeleteStoreTag(id_store, id_tag);
            return NoContent();
        }
    }
}