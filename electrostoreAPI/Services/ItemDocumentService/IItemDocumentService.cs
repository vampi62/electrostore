using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.ItemDocumentService;

public interface IItemDocumentService
{
    public Task<IEnumerable<ReadItemDocumentDto>> GetItemsDocumentsByItemId(int itemId, int limit = 100, int offset = 0);

    public Task<ReadItemDocumentDto> GetItemDocumentById(int id, int? itemId = null);

    public Task<ReadItemDocumentDto> CreateItemDocument(CreateItemDocumentDto itemDocumentDto);

    public Task<ReadItemDocumentDto> UpdateItemDocument(int id, UpdateItemDocumentDto itemDocumentDto, int? itemId = null);

    public Task DeleteItemDocument(int id, int? itemId = null);

    public Task<GetFileResult> GetFile(string url);
}