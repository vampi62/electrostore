using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.BoxService;

public interface IBoxService
{
    public Task<IEnumerable<ReadBoxDto>> GetBoxs(int limit = 100, int offset = 0);

    public Task<ActionResult<IEnumerable<ReadBoxDto>>> GetBoxsByStoreId(int storeId, int limit = 100, int offset = 0);

    public Task<ActionResult<ReadBoxDto>> GetBoxById(int id, int? storeId = null);

    public Task<ActionResult<ReadBoxDto>> CreateBox(CreateBoxDto boxDto);

    public Task<ActionResult<ReadBoxDto>> UpdateBox(int id, UpdateBoxDto boxDto, int? storeId = null);

    public Task<IActionResult> DeleteBox(int id, int? storeId = null);
}