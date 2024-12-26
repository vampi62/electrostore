using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.StoreTagService;
using Swashbuckle.AspNetCore.Annotations;

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
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadExtendedStoreTagDto>>> GetStoresTagsByStoreId([FromRoute] int id_store, [FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery, SwaggerParameter(Description = "Fields to expand. Possible values: 'tag', 'store'. Multiple values can be specified by separating them with ','. Default: \"\"")] string expand = "")
        {
            var storeTags = await _storeTagService.GetStoresTagsByStoreId(id_store, limit, offset, expand.Split(',').ToList());
            var CountList = await _storeTagService.GetStoresTagsCountByStoreId(id_store);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers","X-Total-Count");
            return Ok(storeTags);
        }
        
        [HttpGet("{id_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedStoreTagDto>> GetStoreTagById([FromRoute] int id_store, [FromRoute] int id_tag, [FromQuery, SwaggerParameter(Description = "Fields to expand. Possible values: 'tag', 'store'. Multiple values can be specified by separating them with ','. Default: \"\"")] string expand = "")
        {
            var storeTag = await _storeTagService.GetStoreTagById(id_store, id_tag, expand.Split(',').ToList());
            return Ok(storeTag);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadStoreTagDto>> CreateStoreTag([FromRoute] int id_store, [FromBody] CreateStoreTagByStoreDto storeTagDto)
        {
            var storeTagDtoFull = new CreateStoreTagDto
            {
                id_store = id_store,
                id_tag = storeTagDto.id_tag
            };
            var newStoreTag = await _storeTagService.CreateStoreTag(storeTagDtoFull);
            return CreatedAtAction(nameof(GetStoreTagById), new { id_store = newStoreTag.id_store, id_tag = newStoreTag.id_tag }, newStoreTag);
        }

        [HttpPost("bulk")]
		[Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkStoreTagDto>> CreateBulkStoreTag([FromRoute] int id_store, [FromBody] List<CreateStoreTagByStoreDto> storeTagsDto)
        {
            var storeTagsDtoFull = storeTagsDto.Select(storeTagDto => new CreateStoreTagDto
            {
                id_store = id_store,
                id_tag = storeTagDto.id_tag
            }).ToList();
            var newStoreTags = await _storeTagService.CreateBulkStoreTag(storeTagsDtoFull);
            return Ok(newStoreTags);
        }

        [HttpDelete("{id_tag}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteStoreTag([FromRoute] int id_store, [FromRoute] int id_tag)
        {
            await _storeTagService.DeleteStoreTag(id_store, id_tag);
            return NoContent();
        }

        [HttpDelete("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkStoreTagDto>> DeleteBulkStoreTag([FromRoute] int id_store, [FromBody] List<int> id_tags)
        {
            var itemTagsDtoFull = id_tags.Select(id_item => new CreateStoreTagDto
            {
                id_store = id_store,
                id_tag = id_item
            }).ToList();
            var storeTags = await _storeTagService.DeleteBulkItemTag(itemTagsDtoFull);
            return Ok(storeTags);
        }
    }
}