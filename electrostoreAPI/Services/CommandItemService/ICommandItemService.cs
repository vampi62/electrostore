using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.CommandItemService;

public interface ICommandItemService
{
    public Task<ActionResult<IEnumerable<ReadCommandItemDto>>> GetCommandItemsByCommandId(int commandId, int limit = 100, int offset = 0);

    public Task<ActionResult<IEnumerable<ReadCommandItemDto>>> GetCommandItemsByItemId(int itemId, int limit = 100, int offset = 0);

    public Task<ActionResult<ReadCommandItemDto>> GetCommandItemById(int commandId, int itemId);

    public Task<ActionResult<ReadCommandItemDto>> CreateCommandItem(CreateCommandItemDto commandItemDto);

    public Task<ActionResult<ReadCommandItemDto>> UpdateCommandItem(int commandId, int itemId, UpdateCommandItemDto commandItemDto);

    public Task<IActionResult> DeleteCommandItem(int commandId, int itemId);
}
