using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.ProjectService;

public interface IProjectService
{
    public Task<PaginatedResponseDto<ReadExtendedProjectDto>> GetProjets(int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null, List<int>? idResearch = null);

    public Task<ReadExtendedProjectDto> GetProjetById(int id, List<string>? expand = null);

    public Task<ReadProjectDto> CreateProjet(CreateProjectDto projetDto);

    public Task<ReadProjectDto> UpdateProjet(int id, UpdateProjectDto projetDto);

    public Task DeleteProjet(int id);
}