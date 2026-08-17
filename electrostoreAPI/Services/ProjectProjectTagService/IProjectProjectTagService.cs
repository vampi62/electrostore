using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.ProjectProjectTagService;

public interface IProjectProjectTagService
{
    public Task<PaginatedResponseDto<ReadExtendedProjectProjectTagDto>> GetProjectsProjectTagsByProjectId(int projectId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<PaginatedResponseDto<ReadExtendedProjectProjectTagDto>> GetProjectsProjectTagsByprojectTagId(int projectTagId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<ReadExtendedProjectProjectTagDto> GetProjectProjectTagById(int projectId, int projectTagId, List<string>? expand = null);

    public Task<ReadProjectProjectTagDto> CreateProjectProjectTag(CreateProjectProjectTagDto projectProjectTagDto);

    public Task<ReadBulkProjectProjectTagDto> CreateBulkProjectProjectTag(List<CreateProjectProjectTagDto> projectProjectTagBulkDto);

    public Task DeleteProjectProjectTag(int projectId, int projectTagId);

    public Task<ReadBulkProjectProjectTagDto> DeleteBulkProjectProjectTag(List<CreateProjectProjectTagDto> projectProjectTagBulkDto);
}