using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.ItemService;

public interface IItemService
{
    public Task<IEnumerable<ReadItemDto>> GetItems(int limit = 100, int offset = 0);

    public Task<ReadItemDto> GetItemById(int id);

    public Task<ReadItemDto> CreateItem(CreateItemDto itemDto);

    public Task<ReadItemDto> UpdateItem(int id, UpdateItemDto itemDto);

    public Task DeleteItem(int id);
}