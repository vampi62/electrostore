using electrostore.Dto;

namespace electrostore.Services.ItemTagService;

public interface IItemTagService
{
    public Task<IEnumerable<ReadExtendedItemTagDto>> GetItemsTagsByItemId(int itemId, int limit = 100, int offset = 0, List<string>? expand = null);

    public Task<int> GetItemsTagsCountByItemId(int itemId);

    public Task<IEnumerable<ReadExtendedItemTagDto>> GetItemsTagsByTagId(int tagId, int limit = 100, int offset = 0, List<string>? expand = null);

    public Task<int> GetItemsTagsCountByTagId(int tagId);

    public Task<ReadExtendedItemTagDto> GetItemTagById(int itemId, int tagId, List<string>? expand = null);

    public Task<ReadItemTagDto> CreateItemTag(CreateItemTagDto itemTagDto);

    public Task<ReadBulkItemTagDto> CreateBulkItemTag(List<CreateItemTagDto> itemTagBulkDto);

    public Task DeleteItemTag(int itemId, int tagId);

    public Task<ReadBulkItemTagDto> DeleteBulkItemTag(List<CreateItemTagDto> itemTagBulkDto);
}