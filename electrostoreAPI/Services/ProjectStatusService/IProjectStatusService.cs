using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.ProjectStatusService;

public interface IProjectStatusService
{
    public Task<PaginatedResponseDto<ReadExtendedProjectStatusDto>> GetProjectStatusByProjectId(int projectId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null);

    public Task<ReadExtendedProjectStatusDto> GetProjectStatusById(int id, int? projectId = null);

    public Task<ReadProjectStatusDto> CreateProjectStatus(CreateProjectStatusDto projectStatusDto);
}