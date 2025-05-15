using electrostore.Dto;

namespace electrostore.Services.BoxService;

public interface IBoxService
{
    public Task<IEnumerable<ReadExtendedBoxDto>> GetBoxsByStoreId(int storeId, int limit = 100, int offset = 0, List<string>? expand = null);

    public Task<int> GetBoxsCountByStoreId(int storeId);

    public Task<ReadExtendedBoxDto> GetBoxById(int id, int? storeId = null, List<string>? expand = null);

    public Task<ReadBoxDto> CreateBox(CreateBoxDto boxDto);

    public Task<ReadBulkBoxDto> CreateBulkBox(List<CreateBoxDto> boxsDto);

    public Task<ReadBoxDto> UpdateBox(int id, UpdateBoxDto boxDto, int? storeId = null);

    public Task<ReadBulkBoxDto> UpdateBulkBox(List<UpdateBulkBoxByStoreDto> boxsDto, int? storeId = null);

    public Task DeleteBox(int id, int? storeId = null);

    public Task<ReadBulkBoxDto> DeleteBulkBox(List<int> ids, int storeId);
}