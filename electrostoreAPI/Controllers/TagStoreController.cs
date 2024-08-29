using Microsoft.AspNetCore.Mvc;
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
        public async Task<ActionResult<IEnumerable<ReadStoreTagDto>>> GetStoresTagsByTagId([FromRoute] int id_tag, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var storeTags = await _storeTagService.GetStoresTagsByTagId(id_tag, limit, offset);
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

        [HttpGet("{id_store}")]
        public async Task<ActionResult<ReadStoreTagDto>> GetStoreTagById([FromRoute] int id_tag, [FromRoute] int id_store)
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

        [HttpPost]
        public async Task<ActionResult<ReadStoreTagDto>> CreateStoresTag([FromRoute] int id_tag, [FromBody] int[] stores)
        {
            var resultList = new List<ActionResult<ReadStoreTagDto>>();
            for(int i = 0; i < stores.Length; i++)
            {
                var storeTagDto = new CreateStoreTagDto
                {
                    id_tag = id_tag,
                    id_store = stores[i]
                };
                var storeTag = await _storeTagService.CreateStoreTag(storeTagDto);
                if (storeTag.Result is BadRequestObjectResult || storeTag.Value == null)
                {
                    resultList.Add(storeTag.Result);
                }
                else
                {
                    resultList.Add(storeTag.Value);
                }
            }
            return Ok(resultList);
        }

        [HttpPost("{id_store}")]
        public async Task<ActionResult<ReadStoreTagDto>> CreateStoreTag([FromRoute] int id_tag, [FromRoute] int id_store)
        {
            var storeTagDto = new CreateStoreTagDto
            {
                id_tag = id_tag,
                id_store = id_store
            };
            var storeTag = await _storeTagService.CreateStoreTag(storeTagDto);
            if (storeTag.Result is BadRequestObjectResult)
            {
                return storeTag.Result;
            }
            if (storeTag.Value == null)
            {
                return StatusCode(500);
            }
            return CreatedAtAction(nameof(GetStoreTagById), new { id_tag = storeTag.Value.id_tag, id_store = storeTag.Value.id_store }, storeTag.Value);
        }

        [HttpDelete("{id_store}")]
        public async Task<ActionResult> DeleteStoreTag([FromRoute] int id_tag, [FromRoute] int id_store)
        {
            await _storeTagService.DeleteStoreTag(id_store, id_tag);
            return NoContent();
        }
    }
}