using electrostore.Dto;

namespace electrostore.Services.ItemBoxService;

public interface IItemBoxService
{
    public Task<PaginatedResponseDto<ReadExtendedItemBoxDto>> GetItemsBoxsByBoxId(int boxId, int limit = 100, int offset = 0, List<string>? expand = null);

    public Task<PaginatedResponseDto<ReadExtendedItemBoxDto>> GetItemsBoxsByItemId(int itemId, int limit = 100, int offset = 0, List<string>? expand = null);

    public Task<ReadExtendedItemBoxDto> GetItemBoxById(int itemId, int boxId, List<string>? expand = null);

    public Task<ReadItemBoxDto> CreateItemBox(CreateItemBoxDto itemBoxDto);

    public Task<ReadItemBoxDto> UpdateItemBox(int itemId, int boxId, UpdateItemBoxDto itemBoxDto);

    public Task DeleteItemBox(int itemId, int boxId);

    public Task CheckIfStoreExists(int storeId, int boxId);
}