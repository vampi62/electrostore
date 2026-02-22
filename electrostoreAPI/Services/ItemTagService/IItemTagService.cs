using electrostore.Dto;

namespace electrostore.Services.ItemTagService;

public interface IItemTagService
{
    public Task<PaginatedResponseDto<ReadExtendedItemTagDto>> GetItemsTagsByItemId(int itemId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<PaginatedResponseDto<ReadExtendedItemTagDto>> GetItemsTagsByTagId(int tagId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<ReadExtendedItemTagDto> GetItemTagById(int itemId, int tagId, List<string>? expand = null);

    public Task<ReadItemTagDto> CreateItemTag(CreateItemTagDto itemTagDto);

    public Task<ReadBulkItemTagDto> CreateBulkItemTag(List<CreateItemTagDto> itemTagBulkDto);

    public Task DeleteItemTag(int itemId, int tagId);

    public Task<ReadBulkItemTagDto> DeleteBulkItemTag(List<CreateItemTagDto> itemTagBulkDto);
}