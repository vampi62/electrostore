using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.CommandDocumentService;

public interface ICommandDocumentService
{
    public Task<IEnumerable<ReadCommandDocumentDto>> GetCommandDocumentsByCommandId(int itemId, int limit = 100, int offset = 0);

    public Task<int> GetCommandDocumentsCountByCommandId(int itemId);

    public Task<ReadCommandDocumentDto> GetCommandDocumentById(int id, int? itemId = null);

    public Task<ReadCommandDocumentDto> CreateCommandDocument(CreateCommandDocumentDto itemDocumentDto);

    public Task<ReadCommandDocumentDto> UpdateCommandDocument(int id, UpdateCommandDocumentDto itemDocumentDto, int? commandId = null);

    public Task DeleteCommandDocument(int id, int? itemId = null);

    public Task<GetFileResult> GetFile(string url);
}