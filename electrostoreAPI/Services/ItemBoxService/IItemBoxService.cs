using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.ItemBoxService;

public interface IItemBoxService
{
    public Task<IEnumerable<ReadExtendedItemBoxDto>> GetItemsBoxsByBoxId(int boxId, int limit = 100, int offset = 0, List<string>? expand = null);

    public Task<int> GetItemsBoxsCountByBoxId(int boxId);

    public Task<IEnumerable<ReadExtendedItemBoxDto>> GetItemsBoxsByItemId(int itemId, int limit = 100, int offset = 0, List<string>? expand = null);

    public Task<int> GetItemsBoxsCountByItemId(int itemId);

    public Task<ReadExtendedItemBoxDto> GetItemBoxById(int itemId, int boxId, List<string>? expand = null);

    public Task<ReadItemBoxDto> CreateItemBox(CreateItemBoxDto itemBoxDto);

    public Task<ReadItemBoxDto> UpdateItemBox(int itemId, int boxId, UpdateItemBoxDto itemBoxDto);

    public Task DeleteItemBox(int itemId, int boxId);

    public Task CheckIfStoreExists(int storeId, int boxId);
}