using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.CommandService;

public interface ICommandService
{
    public Task<IEnumerable<ReadCommandDto>> GetCommands(int limit = 100, int offset = 0);

    public Task<ReadCommandDto> GetCommandById(int id);

    public Task<ReadCommandDto> CreateCommand(CreateCommandDto commandDto);

    public Task<ReadCommandDto> UpdateCommand(int id, UpdateCommandDto commandDto);

    public Task DeleteCommand(int id);
}