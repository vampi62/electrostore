using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.ItemBoxService;

public interface IItemBoxService
{
    public Task<ActionResult<IEnumerable<ReadItemBoxDto>>> GetItemsBoxsByBoxId(int boxId, int limit = 100, int offset = 0);

    public Task<ActionResult<IEnumerable<ReadItemBoxDto>>> GetItemsBoxsByItemId(int itemId, int limit = 100, int offset = 0);

    public Task<ActionResult<ReadItemBoxDto>> GetItemBoxById(int itemId, int boxId);

    public Task<ActionResult<ReadItemBoxDto>> CreateItemBox(CreateItemBoxDto itemBoxDto);

    public Task<ActionResult<ReadItemBoxDto>> UpdateItemBox(int itemId, int boxId, UpdateItemBoxDto itemBoxDto);

    public Task<IActionResult> DeleteItemBox(int itemId, int boxId);
}