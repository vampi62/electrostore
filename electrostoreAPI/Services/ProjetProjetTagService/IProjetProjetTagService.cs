using electrostore.Dto;

namespace electrostore.Services.ProjetProjetTagService;

public interface IProjetProjetTagService
{
    public Task<PaginatedResponseDto<ReadExtendedProjetProjetTagDto>> GetProjetsProjetTagsByProjetId(int projetId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<PaginatedResponseDto<ReadExtendedProjetProjetTagDto>> GetProjetsProjetTagsByprojetTagId(int projetTagId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<ReadExtendedProjetProjetTagDto> GetProjetProjetTagById(int projetId, int projetTagId, List<string>? expand = null);

    public Task<ReadProjetProjetTagDto> CreateProjetProjetTag(CreateProjetProjetTagDto projetProjetTagDto);

    public Task<ReadBulkProjetProjetTagDto> CreateBulkProjetProjetTag(List<CreateProjetProjetTagDto> projetProjetTagBulkDto);

    public Task DeleteProjetProjetTag(int projetId, int projetTagId);

    public Task<ReadBulkProjetProjetTagDto> DeleteBulkProjetProjetTag(List<CreateProjetProjetTagDto> projetProjetTagBulkDto);
}