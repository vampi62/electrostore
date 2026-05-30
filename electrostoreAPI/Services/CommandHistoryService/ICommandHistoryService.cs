using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.CommandHistoryService;

public interface ICommandHistoryService
{
    Task<PaginatedResponseDto<ReadCommandHistoryDto>> GetCommandHistoriesByCommandId(int idCommand, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null);
    Task<ReadCommandHistoryDto> GetCommandHistoryById(int idCommandHistory, int? idCommand = null);
}
