using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.CommandService;

public interface ICommandService
{
    public Task<IEnumerable<ReadExtendedCommandDto>> GetCommands(int limit = 100, int offset = 0, List<string>? expand = null, List<int>? idResearch = null);

    public Task<int> GetCommandsCount();

    public Task<ReadExtendedCommandDto> GetCommandById(int id, List<string>? expand = null);

    public Task<ReadCommandDto> CreateCommand(CreateCommandDto commandDto);

    public Task<ReadCommandDto> UpdateCommand(int id, UpdateCommandDto commandDto);

    public Task DeleteCommand(int id);
}