using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.ProjetItemService;

public interface IProjetItemService
{
    public Task<IEnumerable<ReadExtendedProjetItemDto>> GetProjetItemsByProjetId(int projetId, int limit = 100, int offset = 0, List<string>? expand = null);

    public Task<int> GetProjetItemsCountByProjetId(int projetId);

    public Task<IEnumerable<ReadExtendedProjetItemDto>> GetProjetItemsByItemId(int itemId, int limit = 100, int offset = 0, List<string>? expand = null);

    public Task<int> GetProjetItemsCountByItemId(int itemId);

    public Task<ReadExtendedProjetItemDto> GetProjetItemById(int projetId, int itemId, List<string>? expand = null);

    public Task<ReadProjetItemDto> CreateProjetItem(CreateProjetItemDto projetItemDto);

    public Task<ReadBulkProjetItemDto> CreateBulkProjetItem(List<CreateProjetItemDto> projetItemBulkDto);

    public Task<ReadProjetItemDto> UpdateProjetItem(int projetId, int itemId, UpdateProjetItemDto projetItemDto);

    public Task DeleteProjetItem(int projetId, int itemId);
}