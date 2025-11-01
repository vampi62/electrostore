using electrostore.Dto;

namespace electrostore.Services.ProjetTagService;

public interface IProjetTagService
{
    public Task<IEnumerable<ReadExtendedProjetTagDto>> GetProjetTags(int limit = 100, int offset = 0, List<string>? expand = null, List<int>? idResearch = null);

    public Task<int> GetProjetTagsCount();

    public Task<ReadExtendedProjetTagDto> GetProjetTagById(int id, List<string>? expand = null);

    public Task<ReadProjetTagDto> CreateProjetTag(CreateProjetTagDto projetTagDto);

    public Task<ReadBulkProjetTagDto> CreateBulkProjetTag(List<CreateProjetTagDto> projetTagBulkDto);

    public Task<ReadProjetTagDto> UpdateProjetTag(int id, UpdateProjetTagDto projetTagDto);

    public Task DeleteProjetTag(int id);
}