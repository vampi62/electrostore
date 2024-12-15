using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.StoreTagService;

public interface IStoreTagService
{
    public Task<IEnumerable<ReadExtendedStoreTagDto>> GetStoresTagsByStoreId(int storeId, int limit = 100, int offset = 0, List<string>? expand = null);

    public Task<int> GetStoresTagsCountByStoreId(int storeId);

    public Task<IEnumerable<ReadExtendedStoreTagDto>> GetStoresTagsByTagId(int tagId, int limit = 100, int offset = 0, List<string>? expand = null);

    public Task<int> GetStoresTagsCountByTagId(int tagId);

    public Task<ReadExtendedStoreTagDto> GetStoreTagById(int storeId, int tagId, List<string>? expand = null);

    public Task<ReadStoreTagDto> CreateStoreTag(CreateStoreTagDto storeTagDto);

    public Task<ReadBulkStoreTagDto> CreateBulkStoreTag(List<CreateStoreTagDto> storeTagBulkDto);

    public Task DeleteStoreTag(int storeId, int tagId);

    public Task<ReadBulkStoreTagDto> DeleteBulkItemTag(List<CreateStoreTagDto> storeTagBulkDto);
}