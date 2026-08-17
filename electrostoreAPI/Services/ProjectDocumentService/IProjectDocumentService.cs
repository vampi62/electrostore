using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.ProjectDocumentService;

public interface IProjectDocumentService
{
    public Task<PaginatedResponseDto<ReadProjectDocumentDto>> GetProjectDocumentsByProjectId(int projectId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null);

    public Task<ReadProjectDocumentDto> GetProjectDocumentById(int id, int? projectId = null);

    public Task<ReadProjectDocumentDto> CreateProjectDocument(CreateProjectDocumentDto projectDocumentDto);

    public Task<ReadProjectDocumentDto> UpdateProjectDocument(int id, UpdateProjectDocumentDto projectDocumentDto, int? projectId = null);

    public Task DeleteProjectDocument(int id, int? projectId = null);
}