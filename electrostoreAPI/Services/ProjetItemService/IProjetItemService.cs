using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.ProjetItemService;

public interface IProjetItemService
{
    public Task<ActionResult<IEnumerable<ReadProjetItemDto>>> GetProjetItemsByProjetId(int projetId, int limit = 100, int offset = 0);

    public Task<ActionResult<IEnumerable<ReadProjetItemDto>>> GetProjetItemsByItemId(int itemId, int limit = 100, int offset = 0);

    public Task<ActionResult<ReadProjetItemDto>> GetProjetItemById(int projetId, int itemId);

    public Task<ActionResult<ReadProjetItemDto>> CreateProjetItem(CreateProjetItemDto projetItemDto);

    public Task<ActionResult<ReadProjetItemDto>> UpdateProjetItem(int projetId, int itemId, UpdateProjetItemDto projetItemDto);

    public Task<IActionResult> DeleteProjetItem(int projetId, int itemId);
}