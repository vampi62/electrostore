using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.StoreTagService;
using Swashbuckle.AspNetCore.Annotations;

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
        public async Task<ActionResult<IEnumerable<ReadExtendedStoreTagDto>>> GetStoresTagsByTagId([FromRoute] int id_tag, [FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'tag', 'store'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var storeTags = await _storeTagService.GetStoresTagsByTagId(id_tag, limit, offset, expand);
            var CountList = await _storeTagService.GetStoresTagsCountByTagId(id_tag);
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers","X-Total-Count");
            return Ok(storeTags);
        }

        [HttpGet("{id_store}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadExtendedStoreTagDto>> GetStoreTagById([FromRoute] int id_tag, [FromRoute] int id_store, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to expand. Possible values: 'tag', 'store'. Multiple values can be specified by separating them with ','.")] List<string>? expand = null)
        {
            var storeTag = await _storeTagService.GetStoreTagById(id_store, id_tag, expand);
            return Ok(storeTag);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadStoreTagDto>> CreateStoreTag([FromRoute] int id_tag, [FromBody] CreateStoreTagByTagDto storeTagDto)
        {
            var storeTagDtoFull = new CreateStoreTagDto
            {
                id_tag = id_tag,
                id_store = storeTagDto.id_store
            };
            var storeTag = await _storeTagService.CreateStoreTag(storeTagDtoFull);
            return CreatedAtAction(nameof(GetStoreTagById), new { id_tag = storeTag.id_tag, id_store = storeTag.id_store }, storeTag);
        }

        [HttpPost("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkStoreTagDto>> CreateBulkStoreTag([FromRoute] int id_tag, [FromBody] List<CreateStoreTagByTagDto> storeTagsDto)
        {
            var storeTagsDtoFull = storeTagsDto.Select(storeTagDto => new CreateStoreTagDto
            {
                id_tag = id_tag,
                id_store = storeTagDto.id_store
            }).ToList();
            var storeTags = await _storeTagService.CreateBulkStoreTag(storeTagsDtoFull);
            return Ok(storeTags);
        }

        [HttpDelete("{id_store}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteStoreTag([FromRoute] int id_tag, [FromRoute] int id_store)
        {
            await _storeTagService.DeleteStoreTag(id_store, id_tag);
            return NoContent();
        }

        [HttpDelete("bulk")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadBulkStoreTagDto>> DeleteBulkStoreTag([FromRoute] int id_tag, [FromBody] List<int> id_stores)
        {
            var itemTagsDtoFull = id_stores.Select(id_item => new CreateStoreTagDto
            {
                id_tag = id_tag,
                id_store = id_item
            }).ToList();
            var storeTags = await _storeTagService.DeleteBulkItemTag(itemTagsDtoFull);
            return Ok(storeTags);
        }
    }
}