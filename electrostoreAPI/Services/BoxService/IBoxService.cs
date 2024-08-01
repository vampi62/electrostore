using electrostore.Dto;

namespace electrostore.Services.BoxService;

public interface IBoxService
{
    public Task<IEnumerable<ReadBoxDto>> GetBoxs(int limit = 100, int offset = 0);

    public Task<IEnumerable<ReadBoxDto>> GetBoxsByStoreId(int storeId, int limit = 100, int offset = 0);

    public Task<ReadBoxDto> GetBoxById(int id, int? storeId = null);

    public Task<ReadBoxDto> CreateBox(CreateBoxDto boxDto);

    public Task<ReadBoxDto> UpdateBox(int id, UpdateBoxDto boxDto, int? storeId = null);

    public Task DeleteBox(int id, int? storeId = null);
}