using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.CommandDocumentService;

public interface ICommandDocumentService
{
    public Task<IEnumerable<ReadCommandDocumentDto>> GetCommandsDocumentsByCommandId(int commandId, int limit = 100, int offset = 0);

    public Task<int> GetCommandsDocumentsCountByCommandId(int commandId);

    public Task<ReadCommandDocumentDto> GetCommandDocumentById(int id, int? commandId = null);

    public Task<ReadCommandDocumentDto> CreateCommandDocument(CreateCommandDocumentDto commandDocumentDto);

    public Task<ReadCommandDocumentDto> UpdateCommandDocument(int id, UpdateCommandDocumentDto commandDocumentDto, int? commandId = null);

    public Task DeleteCommandDocument(int id, int? commandId = null);
}