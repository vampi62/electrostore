using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.StoreTagService;

public interface IStoreTagService
{
    public Task<IEnumerable<ReadStoreTagDto>> GetStoresTagsByStoreId(int storeId, int limit = 100, int offset = 0);

    public Task<IEnumerable<ReadStoreTagDto>> GetStoresTagsByTagId(int tagId, int limit = 100, int offset = 0);

    public Task<ReadStoreTagDto> GetStoreTagById(int storeId, int tagId);

    public Task<IEnumerable<ReadStoreTagDto>> CreateStoreTags(int? storeId = null, int? tagId = null, int[]? tags = null, int[]? stores = null);

    public Task<ReadStoreTagDto> CreateStoreTag(CreateStoreTagDto storeTagDto);

    public Task DeleteStoreTag(int storeId, int tagId);
}