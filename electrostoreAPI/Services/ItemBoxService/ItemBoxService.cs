using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.ItemHistoryService;
using Microsoft.EntityFrameworkCore;

namespace ElectrostoreAPI.Services.ItemBoxService;

public class ItemBoxService : IItemBoxService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IItemHistoryService _itemHistoryService;

    public ItemBoxService(IMapper mapper, ApplicationDbContext context, IItemHistoryService itemHistoryService)
    {
        _mapper = mapper;
        _context = context;
        _itemHistoryService = itemHistoryService;
    }

    public async Task<PaginatedResponseDto<ReadExtendedItemBoxDto>> GetItemsBoxsByBoxId(int boxId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if the box exists
        if (!await _context.Boxs.AnyAsync(b => b.id_box == boxId))
        {
            throw new KeyNotFoundException($"Box with id '{boxId}' not found");
        }
        var query = _context.ItemsBoxs.AsQueryable();
        rsql ??= [];
        rsql.Add(new FilterDto { field = "id_box", search_type = "eq", value = boxId.ToString() });
        if (expand != null && expand.Contains("item"))
        {
            query = query.Include(ib => ib.Item);
        }
        if (expand != null && expand.Contains("box"))
        {
            query = query.Include(ib => ib.Box);
        }
        return await query
            .ToPagedQuery(limit, offset, rsql, sort, new SorterDto { field = "id_item", order = "asc" })
            .ToResponseAsync(itemBox => _mapper.Map<List<ReadExtendedItemBoxDto>>(itemBox));
    }

    public async Task<PaginatedResponseDto<ReadExtendedItemBoxDto>> GetItemsBoxsByItemId(int itemId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            throw new KeyNotFoundException($"Item with id '{itemId}' not found");
        }
        var query = _context.ItemsBoxs.AsQueryable();
        rsql ??= [];
        rsql.Add(new FilterDto { field = "id_item", search_type = "eq", value = itemId.ToString() });
        if (expand != null && expand.Contains("item"))
        {
            query = query.Include(ib => ib.Item);
        }
        if (expand != null && expand.Contains("box"))
        {
            query = query.Include(ib => ib.Box);
        }
        return await query
            .ToPagedQuery(limit, offset, rsql, sort, new SorterDto { field = "id_box", order = "asc" })
            .ToResponseAsync(itemBox => _mapper.Map<List<ReadExtendedItemBoxDto>>(itemBox));
    }

    public async Task<ReadExtendedItemBoxDto> GetItemBoxById(int itemId, int boxId, List<string>? expand = null)
    {
        var query = _context.ItemsBoxs.AsQueryable();
        query = query.Where(ib => ib.id_box == boxId && ib.id_item == itemId);
        if (expand != null && expand.Contains("item"))
        {
            query = query.Include(ib => ib.Item);
        }
        if (expand != null && expand.Contains("box"))
        {
            query = query.Include(ib => ib.Box);
        }
        var itemBox = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"ItemBox with id '{itemId}' and boxId '{boxId}' not found");
        return _mapper.Map<ReadExtendedItemBoxDto>(itemBox);
    }

    public async Task<ReadItemBoxDto> CreateItemBox(CreateItemBoxDto itemBoxDto)
    {
        // check if the box exists
        if (!await _context.Boxs.AnyAsync(b => b.id_box == itemBoxDto.id_box))
        {
            throw new KeyNotFoundException($"Box with id '{itemBoxDto.id_box}' not found");
        }
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemBoxDto.id_item))
        {
            throw new KeyNotFoundException($"Item with id '{itemBoxDto.id_item}' not found");
        }
        // check if the item is already in the box
        if (await _context.ItemsBoxs.AnyAsync(ib => ib.id_box == itemBoxDto.id_box && ib.id_item == itemBoxDto.id_item))
        {
            throw new InvalidOperationException("Item is already in the box");
        }
        var newItemBox = _mapper.Map<ItemsBoxs>(itemBoxDto);
        _context.ItemsBoxs.Add(newItemBox);
        await _context.SaveChangesAsync();
        await _itemHistoryService.LogHistory(newItemBox.id_item, newItemBox.id_box, ItemHistoryType.StockAdded,
            oldQuantity: null, newQuantity: newItemBox.quantity_item_box);
        return _mapper.Map<ReadItemBoxDto>(newItemBox);
    }

    public async Task<ReadItemBoxDto> UpdateItemBox(int itemId, int boxId, UpdateItemBoxDto itemBoxDto)
    {
        var itemBoxToUpdate = await _context.ItemsBoxs.FindAsync(itemId, boxId) ?? throw new KeyNotFoundException($"ItemBox with id '{itemId}' and boxId '{boxId}' not found");
        var oldQte = itemBoxToUpdate.quantity_item_box;
        if (itemBoxDto.quantity_item_box is not null)
        {
            itemBoxToUpdate.quantity_item_box = itemBoxDto.quantity_item_box.Value;
        }

        if (itemBoxDto.threshold_max_item_item_box is not null)
        {
            itemBoxToUpdate.threshold_max_item_item_box = itemBoxDto.threshold_max_item_item_box.Value;
        }
        await _context.SaveChangesAsync();
        if (itemBoxToUpdate.quantity_item_box != oldQte)
        {
            var historyType = itemBoxToUpdate.quantity_item_box > oldQte
                ? ItemHistoryType.StockAdded
                : ItemHistoryType.StockRemoved;
            await _itemHistoryService.LogHistory(itemId, boxId, historyType,
                oldQuantity: oldQte, newQuantity: itemBoxToUpdate.quantity_item_box);
        }
        return _mapper.Map<ReadItemBoxDto>(itemBoxToUpdate);
    }

    public async Task DeleteItemBox(int itemId, int boxId)
    {
        var itemBoxToDelete = await _context.ItemsBoxs.FindAsync(itemId, boxId) ?? throw new KeyNotFoundException($"ItemBox with id '{itemId}' and boxId '{boxId}' not found");
        await _itemHistoryService.LogHistory(itemBoxToDelete.id_item, itemBoxToDelete.id_box, ItemHistoryType.StockRemoved,
            oldQuantity: itemBoxToDelete.quantity_item_box, newQuantity: null);
        _context.ItemsBoxs.Remove(itemBoxToDelete);
        await _context.SaveChangesAsync();
    }

    public async Task CheckIfStoreExists(int storeId, int boxId)
    {
        if (!await _context.Boxs.AnyAsync(b => b.id_box == boxId && b.id_store == storeId))
        {
            throw new KeyNotFoundException($"Box with id '{boxId}' not found in store with id '{storeId}'");
        }
    }
}