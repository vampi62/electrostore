using electrostore.Dto;

namespace electrostore.Services.ProjetDocumentService;

public interface IProjetDocumentService
{
    public Task<PaginatedResponseDto<ReadProjetDocumentDto>> GetProjetDocumentsByProjetId(int projetId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null);

    public Task<ReadProjetDocumentDto> GetProjetDocumentById(int id, int? projetId = null);

    public Task<ReadProjetDocumentDto> CreateProjetDocument(CreateProjetDocumentDto projetDocumentDto);

    public Task<ReadProjetDocumentDto> UpdateProjetDocument(int id, UpdateProjetDocumentDto projetDocumentDto, int? projetId = null);

    public Task DeleteProjetDocument(int id, int? projetId = null);
}