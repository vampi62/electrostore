using electrostore.Dto;

namespace electrostore.Services.ItemBoxService;

public interface IItemBoxService
{
    public Task<IEnumerable<ReadItemBoxDto>> GetItemsBoxsByBoxId(int boxId, int limit = 100, int offset = 0);

    public Task<IEnumerable<ReadItemBoxDto>> GetItemsBoxsByItemId(int itemId, int limit = 100, int offset = 0);

    public Task<ReadItemBoxDto> GetItemBoxById(int itemId, int boxId);

    public Task<ReadItemBoxDto> CreateItemBox(CreateItemBoxDto itemBoxDto);

    public Task<ReadItemBoxDto> UpdateItemBox(int itemId, int boxId, UpdateItemBoxDto itemBoxDto);

    public Task DeleteItemBox(int itemId, int boxId);
}