using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.LedService;

public interface ILedService
{
    public Task<IEnumerable<ReadLedDto>> GetLeds(int limit = 100, int offset = 0);

    public Task<ActionResult<IEnumerable<ReadLedDto>>> GetLedsByStoreId(int storeId, int limit = 100, int offset = 0);

    public Task<ActionResult<IEnumerable<ReadLedDto>>> GetLedsByStoreIdAndPosition(int storeId, int xmin, int xmax, int ymin, int ymax);

    public Task<ActionResult<ReadLedDto>> GetLedById(int id, int? storeId = null);

    public Task<ActionResult<ReadLedDto>> CreateLed(CreateLedDto ledDto);

    public Task<ActionResult<ReadLedDto>> UpdateLed(int id, UpdateLedDto ledDto, int? storeId = null);

    public Task<IActionResult> DeleteLed(int id, int? storeId = null);

    public Task<IActionResult> ShowLed(ReadLedDto ledDB, int red, int green, int blue, int timeshow, int animation);
    
    public Task<IActionResult> ShowLeds(IEnumerable<ReadLedDto> ledsDB, int red, int green, int blue, int timeshow, int animation);
}