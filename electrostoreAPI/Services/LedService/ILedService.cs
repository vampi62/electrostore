using electrostore.Dto;

namespace electrostore.Services.LedService;

public interface ILedService
{
    public Task<IEnumerable<ReadLedDto>> GetLedsByStoreId(int storeId, int limit = 100, int offset = 0);

    public Task<int> GetLedsCountByStoreId(int storeId);

    public Task<ReadLedDto> GetLedById(int id, int? storeId = null);

    public Task<ReadLedDto> CreateLed(CreateLedDto ledDto);

    public Task<ReadBulkLedDto> CreateBulkLed(List<CreateLedDto> ledsDto);

    public Task<ReadLedDto> UpdateLed(int id, UpdateLedDto ledDto, int? storeId = null);

    public Task<ReadBulkLedDto> UpdateBulkLed(List<UpdateBulkLedStoreDto> ledsDto, int storeId);

    public Task DeleteLed(int id, int? storeId = null);

    public Task<ReadBulkLedDto> DeleteBulkLed(List<int> ids, int storeId);
    
    public Task ShowLedById(int storeId, int id, int redColor, int greenColor, int blueColor, int timeshow, int animation);
    
    public Task ShowLedsByBox(int storeId, int boxId, int redColor, int greenColor, int blueColor, int timeshow, int animation);
}