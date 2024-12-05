using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.ProjetDocumentService;

public interface IProjetDocumentService
{
    public Task<IEnumerable<ReadProjetDocumentDto>> GetProjetDocumentsByProjetId(int projetId, int limit = 100, int offset = 0);

    public Task<int> GetProjetDocumentsCountByProjetId(int projetId);

    public Task<ReadProjetDocumentDto> GetProjetDocumentById(int id, int? projetId = null);

    public Task<ReadProjetDocumentDto> CreateProjetDocument(CreateProjetDocumentDto projetDocumentDto);

    public Task<ReadProjetDocumentDto> UpdateProjetDocument(int id, UpdateProjetDocumentDto projetDocumentDto, int? projetId = null);

    public Task DeleteProjetDocument(int id, int? projetId = null);

    public Task<GetFileResult> GetFile(string url);
}