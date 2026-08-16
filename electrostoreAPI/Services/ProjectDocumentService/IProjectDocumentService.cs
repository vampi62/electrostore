using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.ProjectDocumentService;

public interface IProjectDocumentService
{
    public Task<PaginatedResponseDto<ReadProjectDocumentDto>> GetProjetDocumentsByProjetId(int projetId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null);

    public Task<ReadProjectDocumentDto> GetProjetDocumentById(int id, int? projetId = null);

    public Task<ReadProjectDocumentDto> CreateProjetDocument(CreateProjectDocumentDto projetDocumentDto);

    public Task<ReadProjectDocumentDto> UpdateProjetDocument(int id, UpdateProjectDocumentDto projetDocumentDto, int? projetId = null);

    public Task DeleteProjetDocument(int id, int? projetId = null);
}