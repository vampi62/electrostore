using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.CommandItemService;

public interface ICommandItemService
{
    public Task<IEnumerable<ReadExtendedCommandItemDto>> GetCommandsItemsByCommandId(int commandId, int limit = 100, int offset = 0, List<string>? expand = null);

    public Task<int> GetCommandsItemsCountByCommandId(int commandId);

    public Task<IEnumerable<ReadExtendedCommandItemDto>> GetCommandsItemsByItemId(int itemId, int limit = 100, int offset = 0, List<string>? expand = null);

    public Task<int> GetCommandsItemsCountByItemId(int itemId);

    public Task<ReadExtendedCommandItemDto> GetCommandItemById(int commandId, int itemId, List<string>? expand = null);

    public Task<ReadCommandItemDto> CreateCommandItem(CreateCommandItemDto commandItemDto);

    public Task<ReadBulkCommandItemDto> CreateBulkCommandItem(List<CreateCommandItemDto> commandItemBulkDto);

    public Task<ReadCommandItemDto> UpdateCommandItem(int commandId, int itemId, UpdateCommandItemDto commandItemDto);

    public Task DeleteCommandItem(int commandId, int itemId);
}
