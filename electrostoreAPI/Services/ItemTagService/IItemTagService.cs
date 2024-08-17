using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.ItemTagService;

public interface IItemTagService
{
    public Task<ActionResult<IEnumerable<ReadItemTagDto>>> GetItemsTagsByItemId(int itemId, int limit = 100, int offset = 0);

    public Task<ActionResult<IEnumerable<ReadItemTagDto>>> GetItemsTagsByTagId(int tagId, int limit = 100, int offset = 0);

    public Task<ActionResult<ReadItemTagDto>> GetItemTagById(int itemId, int tagId);

    public Task<ActionResult<ReadItemTagDto>> CreateItemTag(CreateItemTagDto itemTagDto);

    public Task<IActionResult> DeleteItemTag(int itemId, int tagId);
}