using electrostore.Dto;

namespace electrostore.Services.ItemDocumentService;

public interface IItemDocumentService
{
    public Task<PaginatedResponseDto<ReadItemDocumentDto>> GetItemsDocumentsByItemId(int itemId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null);

    public Task<ReadItemDocumentDto> GetItemDocumentById(int id, int? itemId = null);

    public Task<ReadItemDocumentDto> CreateItemDocument(CreateItemDocumentDto itemDocumentDto);

    public Task<ReadItemDocumentDto> UpdateItemDocument(int id, UpdateItemDocumentDto itemDocumentDto, int? itemId = null);

    public Task DeleteItemDocument(int id, int? itemId = null);
}