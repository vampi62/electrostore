using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.CommandHistoryService;

public interface ICommandHistoryService
{
    public Task<PaginatedResponseDto<ReadCommandHistoryDto>> GetCommandHistoryByCommandId(int idCommand, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null);

    public Task<ReadCommandHistoryDto> GetCommandHistoryById(int id, int idCommand);

    public Task<ReadCommandHistoryDto> CreateCommandHistory(CreateCommandHistoryDto commandHistoryDto);
}
