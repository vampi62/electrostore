using electrostore.Dto;

namespace electrostore.Services.ProjetItemService;

public interface IProjetItemService
{
    public Task<PaginatedResponseDto<ReadExtendedProjetItemDto>> GetProjetItemsByProjetId(int projetId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<PaginatedResponseDto<ReadExtendedProjetItemDto>> GetProjetItemsByItemId(int itemId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<ReadExtendedProjetItemDto> GetProjetItemById(int projetId, int itemId, List<string>? expand = null);

    public Task<ReadProjetItemDto> CreateProjetItem(CreateProjetItemDto projetItemDto);

    public Task<ReadBulkProjetItemDto> CreateBulkProjetItem(List<CreateProjetItemDto> projetItemBulkDto);

    public Task<ReadProjetItemDto> UpdateProjetItem(int projetId, int itemId, UpdateProjetItemDto projetItemDto);

    public Task DeleteProjetItem(int projetId, int itemId);
}