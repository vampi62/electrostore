using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.ProjectStatusService;

public interface IProjectStatusService
{
    public Task<PaginatedResponseDto<ReadExtendedProjectStatusDto>> GetProjetStatusByProjetId(int projetId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null);

    public Task<ReadExtendedProjectStatusDto> GetProjetStatusById(int id, int? projetId = null);

    public Task<ReadProjectStatusDto> CreateProjetStatus(CreateProjectStatusDto projetStatusDto);
}