using electrostore.Dto;

namespace electrostore.Services.StoreService;

public interface IStoreService
{
    public Task<IEnumerable<ReadStoreDto>> GetStores(int limit = 100, int offset = 0);

    public Task<ReadStoreDto> GetStoreById(int id);

    public Task<ReadStoreDto> CreateStore(CreateStoreDto storeDto);

    public Task<ReadStoreDto> UpdateStore(int id, UpdateStoreDto storeDto);

    public Task DeleteStore(int id);
}