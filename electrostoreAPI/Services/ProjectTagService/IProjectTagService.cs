using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.ProjectTagService;

public interface IProjectTagService
{
    public Task<PaginatedResponseDto<ReadExtendedProjectTagDto>> GetProjetTags(int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null, List<int>? idResearch = null);

    public Task<ReadExtendedProjectTagDto> GetProjetTagById(int id, List<string>? expand = null);

    public Task<ReadProjectTagDto> CreateProjetTag(CreateProjectTagDto projetTagDto);

    public Task<ReadBulkProjectTagDto> CreateBulkProjetTag(List<CreateProjectTagDto> projetTagBulkDto);

    public Task<ReadProjectTagDto> UpdateProjetTag(int id, UpdateProjectTagDto projetTagDto);

    public Task DeleteProjetTag(int id);
}