using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.ProjetItemService;

public interface IProjetItemService
{
    public Task<IEnumerable<ReadProjetItemDto>> GetProjetItemsByProjetId(int projetId, int limit = 100, int offset = 0);

    public Task<IEnumerable<ReadProjetItemDto>> GetProjetItemsByItemId(int itemId, int limit = 100, int offset = 0);

    public Task<ReadProjetItemDto> GetProjetItemById(int projetId, int itemId);

    public Task<ReadProjetItemDto> CreateProjetItem(CreateProjetItemDto projetItemDto);

    public Task<ReadProjetItemDto> UpdateProjetItem(int projetId, int itemId, UpdateProjetItemDto projetItemDto);

    public Task DeleteProjetItem(int projetId, int itemId);
}