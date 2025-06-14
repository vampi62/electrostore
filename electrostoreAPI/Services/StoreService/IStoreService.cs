using electrostore.Dto;

namespace electrostore.Services.StoreService;

public interface IStoreService
{
    public Task<IEnumerable<ReadExtendedStoreDto>> GetStores(int limit = 100, int offset = 0, List<string>? expand = null, List<int>? idResearch = null);

    public Task<int> GetStoresCount();

    public Task<ReadExtendedStoreDto> GetStoreById(int id, List<string>? expand = null);

    public Task<ReadStoreDto> CreateStore(CreateStoreDto storeDto);

    public Task<ReadStoreDto> UpdateStore(int id, UpdateStoreDto storeDto);

    public Task DeleteStore(int id);

    public Task<ReadStoreCompleteDto> CreateStoreComplete(CreateStoreCompleteDto storeDto);

    public Task<ReadStoreCompleteDto> UpdateStoreComplete(int id, UpdateStoreCompleteDto storeDto);
}