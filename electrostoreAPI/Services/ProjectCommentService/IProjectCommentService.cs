using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.ProjectCommentService;

public interface IProjectCommentService
{
    public Task<PaginatedResponseDto<ReadExtendedProjectCommentDto>> GetProjectCommentsByProjectId(int projectId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<PaginatedResponseDto<ReadExtendedProjectCommentDto>> GetProjectCommentsByUserId(int userId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<ReadExtendedProjectCommentDto> GetProjectCommentsById(int id, int? userId = null, int? projectId = null, List<string>? expand = null);

    public Task<ReadProjectCommentDto> CreateProjectComment(CreateProjectCommentDto projectCommentDto);

    public Task<ReadProjectCommentDto> UpdateProjectComment(int id, UpdateProjectCommentDto projectCommentDto, int? userId = null, int? projectId = null);

    public Task DeleteProjectComment(int id, int? userId = null, int? projectId = null);
}