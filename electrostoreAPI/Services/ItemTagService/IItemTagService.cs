using electrostore.Dto;

namespace electrostore.Services.ItemTagService;

public interface IItemTagService
{
    public Task<IEnumerable<ReadItemTagDto>> GetItemsTagsByItemId(int itemId, int limit = 100, int offset = 0);

    public Task<IEnumerable<ReadItemTagDto>> GetItemsTagsByTagId(int tagId, int limit = 100, int offset = 0);

    public Task<ReadItemTagDto> GetItemTagById(int itemId, int tagId);

    public Task<ReadItemTagDto> CreateItemTag(CreateItemTagDto itemTagDto);

    public Task DeleteItemTag(int itemId, int tagId);
}