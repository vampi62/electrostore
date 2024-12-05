using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.LedService;

public interface ILedService
{
    public Task<IEnumerable<ReadLedDto>> GetLedsByStoreId(int storeId, int limit = 100, int offset = 0);

    public Task<int> GetLedsCountByStoreId(int storeId);

    public Task<IEnumerable<ReadLedDto>> GetLedsByStoreIdAndPosition(int storeId, int xmin, int xmax, int ymin, int ymax);

    public Task<ReadLedDto> GetLedById(int id, int? storeId = null);

    public Task<ReadLedDto> CreateLed(CreateLedDto ledDto);

    public Task<ReadLedDto> UpdateLed(int id, UpdateLedDto ledDto, int? storeId = null);

    public Task DeleteLed(int id, int? storeId = null);

    public Task ShowLed(ReadLedDto ledDB, int red, int green, int blue, int timeshow, int animation);
    
    public Task ShowLeds(IEnumerable<ReadLedDto> ledsDB, int red, int green, int blue, int timeshow, int animation);
}