using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;

namespace ElectrostoreAPI.Services.ItemHistoryService;

public interface IItemHistoryService
{
    Task<PaginatedResponseDto<ReadExtendedItemHistoryDto>> GetItemHistoryByItemId(int itemId, int limit = 100, int offset = 0,
        List<FilterDto>? rsql = null, SorterDto? sort = null);

    Task<ReadExtendedItemHistoryDto> GetItemHistoryById(int id, int itemId);

    Task<PaginatedResponseDto<ReadExtendedItemHistoryDto>> GetItemsHistory(int limit = 100, int offset = 0,
        List<FilterDto>? rsql = null, SorterDto? sort = null);

    Task LogHistory(int? itemId, int? boxId, ItemHistoryType type,
        int? oldQuantity = null, int? newQuantity = null, string? notes = null);
}
