using electrostore.Dto;

namespace electrostore.Services.ProjetService;

public interface IProjetService
{
    public Task<PaginatedResponseDto<ReadExtendedProjetDto>> GetProjets(int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null, List<int>? idResearch = null);

    public Task<ReadExtendedProjetDto> GetProjetById(int id, List<string>? expand = null);

    public Task<ReadProjetDto> CreateProjet(CreateProjetDto projetDto);

    public Task<ReadProjetDto> UpdateProjet(int id, UpdateProjetDto projetDto);

    public Task DeleteProjet(int id);
}