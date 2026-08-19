using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.ItemVendorService;

public interface IItemVendorService
{
    Task<PaginatedResponseDto<ReadExtendedItemVendorDto>> GetItemVendorsByItemId(int itemId, int limit = 100, int offset = 0,
        List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    Task<ReadExtendedItemVendorDto> GetItemVendorById(int id, int? itemId = null, List<string>? expand = null);

    Task<PaginatedResponseDto<ReadExtendedItemVendorDto>> GetItemVendors(int limit = 100, int offset = 0,
        List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    Task<ReadItemVendorDto> CreateItemVendor(CreateItemVendorDto itemVendorDto);

    Task<ReadItemVendorDto> UpdateItemVendor(int id, UpdateItemVendorDto itemVendorDto, int? itemId = null);

    Task DeleteItemVendor(int id, int? itemId = null);
}
