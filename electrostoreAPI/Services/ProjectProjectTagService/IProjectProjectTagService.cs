using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.ProjectProjectTagService;

public interface IProjectProjectTagService
{
    public Task<PaginatedResponseDto<ReadExtendedProjectProjectTagDto>> GetProjetsProjetTagsByProjetId(int projetId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<PaginatedResponseDto<ReadExtendedProjectProjectTagDto>> GetProjetsProjetTagsByprojetTagId(int projetTagId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<ReadExtendedProjectProjectTagDto> GetProjetProjetTagById(int projetId, int projetTagId, List<string>? expand = null);

    public Task<ReadProjectProjectTagDto> CreateProjetProjetTag(CreateProjectProjectTagDto projetProjetTagDto);

    public Task<ReadBulkProjectProjectTagDto> CreateBulkProjetProjetTag(List<CreateProjectProjectTagDto> projetProjetTagBulkDto);

    public Task DeleteProjetProjetTag(int projetId, int projetTagId);

    public Task<ReadBulkProjectProjectTagDto> DeleteBulkProjetProjetTag(List<CreateProjectProjectTagDto> projetProjetTagBulkDto);
}