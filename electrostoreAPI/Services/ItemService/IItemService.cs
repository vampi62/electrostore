using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.ItemService;

public interface IItemService
{
    public Task<IEnumerable<ReadExtendedItemDto>> GetItems(int limit = 100, int offset = 0, List<string>? expand = null, List<int>? idResearch = null);

    public Task<int> GetItemsCount();

    public Task<ReadExtendedItemDto> GetItemById(int id, List<string>? expand = null);

    public Task<ReadItemDto> CreateItem(CreateItemDto itemDto);

    public Task<ReadItemDto> UpdateItem(int id, UpdateItemDto itemDto);

    public Task DeleteItem(int id);
}