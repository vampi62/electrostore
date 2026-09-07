using ElectrostoreAPI.Dto;
using Microsoft.AspNetCore.Mvc;

namespace ElectrostoreAPI.Services.ItemService;

public interface IItemService
{
    public Task<PaginatedResponseDto<ReadExtendedItemDto>> GetItems(int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null, List<int>? idResearch = null);

    public Task<ReadExtendedItemDto> GetItemById(int id, List<string>? expand = null);

    public Task<ReadItemDto> CreateItem(CreateItemDto itemDto);

    public Task<ReadItemDto> UpdateItem(int id, UpdateItemDto itemDto);

    public Task DeleteItem(int id);

    /// <param name="sinceDate">
    /// Lorsque renseignée, ne retourne que les items sous leur seuil ayant également une entrée
    /// ItemsHistory de type changement de quantité (StockAdded/StockRemoved/StockUpdated) créée
    /// à partir de cette date. Lorsqu'omise, retourne tous les items sous leur seuil.
    /// </param>
    public Task<IEnumerable<ReadItemDto>> GetLowStockItemsAsync(DateTime? sinceDate = null, CancellationToken cancellationToken = default);
}