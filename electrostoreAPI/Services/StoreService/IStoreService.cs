using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.StoreService;

public interface IStoreService
{
    public Task<IEnumerable<ReadStoreDto>> GetStores(int limit = 100, int offset = 0);

    public Task<ActionResult<ReadStoreDto>> GetStoreById(int id);

    public Task<ActionResult<ReadStoreDto>> CreateStore(CreateStoreDto storeDto);

    public Task<ActionResult<ReadStoreDto>> UpdateStore(int id, UpdateStoreDto storeDto);

    public Task<IActionResult> DeleteStore(int id);
}