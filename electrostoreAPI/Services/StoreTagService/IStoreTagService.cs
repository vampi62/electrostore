using electrostore.Dto;

namespace electrostore.Services.StoreTagService;

public interface IStoreTagService
{
    public Task<PaginatedResponseDto<ReadExtendedStoreTagDto>> GetStoresTagsByStoreId(int storeId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);
    public Task<PaginatedResponseDto<ReadExtendedStoreTagDto>> GetStoresTagsByTagId(int tagId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<ReadExtendedStoreTagDto> GetStoreTagById(int storeId, int tagId, List<string>? expand = null);

    public Task<ReadStoreTagDto> CreateStoreTag(CreateStoreTagDto storeTagDto);

    public Task<ReadBulkStoreTagDto> CreateBulkStoreTag(List<CreateStoreTagDto> storeTagBulkDto);

    public Task DeleteStoreTag(int storeId, int tagId);

    public Task<ReadBulkStoreTagDto> DeleteBulkStoreTag(List<CreateStoreTagDto> storeTagBulkDto);
}