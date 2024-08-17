using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.StoreTagService;

public interface IStoreTagService
{
    public Task<ActionResult<IEnumerable<ReadStoreTagDto>>> GetStoresTagsByStoreId(int storeId, int limit = 100, int offset = 0);

    public Task<ActionResult<IEnumerable<ReadStoreTagDto>>> GetStoresTagsByTagId(int tagId, int limit = 100, int offset = 0);

    public Task<ActionResult<ReadStoreTagDto>> GetStoreTagById(int storeId, int tagId);

    public Task<ActionResult<ReadStoreTagDto>> CreateStoreTag(CreateStoreTagDto storeTagDto);

    public Task<IActionResult> DeleteStoreTag(int storeId, int tagId);
}