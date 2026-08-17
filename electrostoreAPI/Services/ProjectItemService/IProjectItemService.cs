using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.ProjectItemService;

public interface IProjectItemService
{
    public Task<PaginatedResponseDto<ReadExtendedProjectItemDto>> GetProjectItemsByProjectId(int projectId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<PaginatedResponseDto<ReadExtendedProjectItemDto>> GetProjectItemsByItemId(int itemId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<ReadExtendedProjectItemDto> GetProjectItemById(int projectId, int itemId, List<string>? expand = null);

    public Task<ReadProjectItemDto> CreateProjectItem(CreateProjectItemDto projectItemDto);

    public Task<ReadBulkProjectItemDto> CreateBulkProjectItem(List<CreateProjectItemDto> projectItemBulkDto);

    public Task<ReadProjectItemDto> UpdateProjectItem(int projectId, int itemId, UpdateProjectItemDto projectItemDto);

    public Task DeleteProjectItem(int projectId, int itemId);
}