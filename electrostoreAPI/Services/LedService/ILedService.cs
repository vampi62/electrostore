using electrostore.Dto;

namespace electrostore.Services.LedService;

public interface ILedService
{
    public Task<IEnumerable<ReadLedDto>> GetLeds(int limit = 100, int offset = 0);

    public Task<IEnumerable<ReadLedDto>> GetLedsByStoreId(int storeId, int limit = 100, int offset = 0);

    public Task<ReadLedDto> GetLedById(int id, int? storeId = null);

    public Task<ReadLedDto> CreateLed(CreateLedDto ledDto);

    public Task<ReadLedDto> UpdateLed(int id, UpdateLedDto ledDto, int? storeId = null);

    public Task DeleteLed(int id, int? storeId = null);
}