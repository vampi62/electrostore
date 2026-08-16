using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.ProjectService;

public interface IProjectService
{
    public Task<PaginatedResponseDto<ReadExtendedProjectDto>> GetProjects(int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null, List<int>? idResearch = null);

    public Task<ReadExtendedProjectDto> GetProjectById(int id, List<string>? expand = null);

    public Task<ReadProjectDto> CreateProject(CreateProjectDto projectDto);

    public Task<ReadProjectDto> UpdateProject(int id, UpdateProjectDto projectDto);

    public Task DeleteProject(int id);
}