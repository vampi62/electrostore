using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.ItemService;

public interface IItemService
{
    public Task<IEnumerable<ReadItemDto>> GetItems(int limit = 100, int offset = 0);

    public Task<ActionResult<ReadItemDto>> GetItemById(int id);

    public Task<ActionResult<ReadItemDto>> CreateItem(CreateItemDto itemDto);

    public Task<ActionResult<ReadItemDto>> UpdateItem(int id, UpdateItemDto itemDto);

    public Task<IActionResult> DeleteItem(int id);
}