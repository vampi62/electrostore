using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.ProjectTagService;

public interface IProjectTagService
{
    public Task<PaginatedResponseDto<ReadExtendedProjectTagDto>> GetProjectTags(int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null, List<int>? idResearch = null);

    public Task<ReadExtendedProjectTagDto> GetProjectTagById(int id, List<string>? expand = null);

    public Task<ReadProjectTagDto> CreateProjectTag(CreateProjectTagDto projectTagDto);

    public Task<ReadBulkProjectTagDto> CreateBulkProjectTag(List<CreateProjectTagDto> projectTagBulkDto);

    public Task<ReadProjectTagDto> UpdateProjectTag(int id, UpdateProjectTagDto projectTagDto);

    public Task DeleteProjectTag(int id);
}