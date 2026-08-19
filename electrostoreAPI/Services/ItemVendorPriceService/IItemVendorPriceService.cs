using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.ItemVendorPriceService;

public interface IItemVendorPriceService
{
    Task<PaginatedResponseDto<ReadExtendedItemVendorPriceDto>> GetPriceHistoryByItemVendorId(int itemVendorId, int limit = 100, int offset = 0,
        List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    Task<ReadExtendedItemVendorPriceDto> GetPriceHistoryById(int id, int? itemVendorId = null, List<string>? expand = null);

    Task<PaginatedResponseDto<ReadExtendedItemVendorPriceDto>> GetPriceHistory(int limit = 100, int offset = 0,
        List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    Task<ReadItemVendorPriceDto> RecordPriceObservation(int idItemVendor, float price, string currency,
        int quantity = 1, string? priceBreaksJson = null);
}
