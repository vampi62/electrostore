using electrostore.Dto;

namespace electrostore.Services.ProjetStatusService;

public interface IProjetStatusService
{
    public Task<IEnumerable<ReadExtendedProjetStatusDto>> GetProjetStatusByProjetId(int projetId, int limit = 100, int offset = 0, List<string>? expand = null);

    public Task<int> GetProjetStatusCountByProjetId(int projetId);

    public Task<ReadExtendedProjetStatusDto> GetProjetStatusById(int id, int? userId = null, int? projetId = null, List<string>? expand = null);

    public Task<ReadProjetStatusDto> CreateProjetStatus(CreateProjetStatusDto projetStatusDto);
}