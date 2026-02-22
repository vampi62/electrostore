using electrostore.Dto;

namespace electrostore.Services.ProjetStatusService;

public interface IProjetStatusService
{
    public Task<PaginatedResponseDto<ReadExtendedProjetStatusDto>> GetProjetStatusByProjetId(int projetId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null);

    public Task<ReadExtendedProjetStatusDto> GetProjetStatusById(int id, int? projetId = null);

    public Task<ReadProjetStatusDto> CreateProjetStatus(CreateProjetStatusDto projetStatusDto);
}