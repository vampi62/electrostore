using electrostore.Dto;

namespace electrostore.Services.CommandItemService;

public interface ICommandItemService
{
    public Task<IEnumerable<ReadCommandItemDto>> GetCommandItemsByCommandId(int commandId, int limit = 100, int offset = 0);

    public Task<IEnumerable<ReadCommandItemDto>> GetCommandItemsByItemId(int itemId, int limit = 100, int offset = 0);

    public Task<ReadCommandItemDto> GetCommandItemById(int commandId, int itemId);

    public Task<ReadCommandItemDto> CreateCommandItem(CreateCommandItemDto commandItemDto);

    public Task<ReadCommandItemDto> UpdateCommandItem(int commandId, int itemId, UpdateCommandItemDto commandItemDto);

    public Task DeleteCommandItem(int commandId, int itemId);
}
