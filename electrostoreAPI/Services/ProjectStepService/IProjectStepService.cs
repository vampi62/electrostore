using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.ProjectStepService;

public interface IProjectStepService
{
    public Task<PaginatedResponseDto<ReadExtendedProjectStepDto>> GetProjectStepsByProjectId(int projectId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<ReadExtendedProjectStepDto> GetProjectStepById(int id, int? projectId = null, List<string>? expand = null);

    public Task<ReadProjectStepDto> CreateProjectStep(CreateProjectStepDto projectStepDto);

    public Task<ReadProjectStepDto> UpdateProjectStep(int id, UpdateProjectStepDto projectStepDto, int? projectId = null);

    public Task DeleteProjectStep(int id, int? projectId = null);
}
