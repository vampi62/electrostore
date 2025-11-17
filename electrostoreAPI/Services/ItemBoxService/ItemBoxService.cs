using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.ItemBoxService;

public class ItemBoxService : IItemBoxService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public ItemBoxService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<IEnumerable<ReadExtendedItemBoxDto>> GetItemsBoxsByBoxId(int boxId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if the box exists
        if (!await _context.Boxs.AnyAsync(b => b.id_box == boxId))
        {
            throw new KeyNotFoundException($"Box with id '{boxId}' not found");
        }
        var query = _context.ItemsBoxs.AsQueryable();
        query = query.Where(ib => ib.id_box == boxId);
        query = query.Skip(offset).Take(limit);
        query = query.OrderBy(ib => ib.id_item);
        if (expand != null && expand.Contains("item"))
        {
            query = query.Include(ib => ib.Item);
        }
        if (expand != null && expand.Contains("box"))
        {
            query = query.Include(ib => ib.Box);
        }
        var itemBox = await query.ToListAsync();
        return _mapper.Map<List<ReadExtendedItemBoxDto>>(itemBox);
    }

    public async Task<int> GetItemsBoxsCountByBoxId(int boxId)
    {
        // check if the box exists
        if (!await _context.Boxs.AnyAsync(b => b.id_box == boxId))
        {
            throw new KeyNotFoundException($"Box with id '{boxId}' not found");
        }
        return await _context.ItemsBoxs
            .CountAsync(ib => ib.id_box == boxId);
    }

    public async Task<IEnumerable<ReadExtendedItemBoxDto>> GetItemsBoxsByItemId(int itemId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            throw new KeyNotFoundException($"Item with id '{itemId}' not found");
        }
        var query = _context.ItemsBoxs.AsQueryable();
        query = query.Where(ib => ib.id_item == itemId);
        query = query.Skip(offset).Take(limit);
        query = query.OrderBy(ib => ib.id_box);
        if (expand != null && expand.Contains("item"))
        {
            query = query.Include(ib => ib.Item);
        }
        if (expand != null && expand.Contains("box"))
        {
            query = query.Include(ib => ib.Box);
        }
        var itemBox = await query.ToListAsync();
        return _mapper.Map<List<ReadExtendedItemBoxDto>>(itemBox);
    }

    public async Task<int> GetItemsBoxsCountByItemId(int itemId)
    {
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            throw new KeyNotFoundException($"Item with id '{itemId}' not found");
        }
        return await _context.ItemsBoxs
            .CountAsync(ib => ib.id_item == itemId);
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
        return _mapper.Map<ReadItemBoxDto>(newItemBox);
    }

    public async Task<ReadItemBoxDto> UpdateItemBox(int itemId, int boxId, UpdateItemBoxDto itemBoxDto)
    {
        var itemBoxToUpdate = await _context.ItemsBoxs.FindAsync(itemId, boxId) ?? throw new KeyNotFoundException($"ItemBox with id '{itemId}' and boxId '{boxId}' not found");
        if (itemBoxDto.qte_item_box is not null)
        {
            itemBoxToUpdate.qte_item_box = itemBoxDto.qte_item_box.Value;
        }

        if (itemBoxDto.seuil_max_item_item_box is not null)
        {
            itemBoxToUpdate.seuil_max_item_item_box = itemBoxDto.seuil_max_item_item_box.Value;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadItemBoxDto>(itemBoxToUpdate);
    }

    public async Task DeleteItemBox(int itemId, int boxId)
    {
        var itemBoxToDelete = await _context.ItemsBoxs.FindAsync(itemId, boxId) ?? throw new KeyNotFoundException($"ItemBox with id '{itemId}' and boxId '{boxId}' not found");
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