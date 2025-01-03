using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.ItemBoxService;

public class ItemBoxService : IItemBoxService
{
    private readonly ApplicationDbContext _context;

    public ItemBoxService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReadExtendedItemBoxDto>> GetItemsBoxsByBoxId(int boxId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if the box exists
        if (!await _context.Boxs.AnyAsync(box => box.id_box == boxId))
        {
            throw new KeyNotFoundException($"Box with id {boxId} not found");
        }
        return await _context.ItemsBoxs
            .Skip(offset)
            .Take(limit)
            .Where(itemBox => itemBox.id_box == boxId)
            .Select(itemBox => new ReadExtendedItemBoxDto
            {
                id_box = itemBox.id_box,
                id_item = itemBox.id_item,
                qte_item_box = itemBox.qte_item_box,
                seuil_max_item_item_box = itemBox.seuil_max_item_item_box,
                item = expand != null && expand.Contains("item") ? new ReadItemDto
                {
                    id_item = itemBox.Item.id_item,
                    nom_item = itemBox.Item.nom_item,
                    seuil_min_item = itemBox.Item.seuil_min_item,
                    description_item = itemBox.Item.description_item,
                    id_img = itemBox.Item.id_img
                } : null,
                box = expand != null && expand.Contains("box") ? new ReadBoxDto
                {
                    id_box = itemBox.Box.id_box,
                    xstart_box = itemBox.Box.xstart_box,
                    ystart_box = itemBox.Box.ystart_box,
                    xend_box = itemBox.Box.xend_box,
                    yend_box = itemBox.Box.yend_box,
                    id_store = itemBox.Box.id_store
                } : null
            })
            .ToListAsync();
    }

    public async Task<int> GetItemsBoxsCountByBoxId(int boxId)
    {
        // check if the box exists
        if (!await _context.Boxs.AnyAsync(box => box.id_box == boxId))
        {
            throw new KeyNotFoundException($"Box with id {boxId} not found");
        }
        return await _context.ItemsBoxs
            .CountAsync(itemBox => itemBox.id_box == boxId);
    }

    public async Task<IEnumerable<ReadExtendedItemBoxDto>> GetItemsBoxsByItemId(int ItemId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if the item exists
        if (!await _context.Items.AnyAsync(item => item.id_item == ItemId))
        {
            throw new KeyNotFoundException($"Item with id {ItemId} not found");
        }
        return await _context.ItemsBoxs
            .Skip(offset)
            .Take(limit)
            .Where(itemBox => itemBox.id_item == ItemId)
            .Select(itemBox => new ReadExtendedItemBoxDto
            {
                id_box = itemBox.id_box,
                id_item = itemBox.id_item,
                qte_item_box = itemBox.qte_item_box,
                seuil_max_item_item_box = itemBox.seuil_max_item_item_box,
                item = expand != null && expand.Contains("item") ? new ReadItemDto
                {
                    id_item = itemBox.Item.id_item,
                    nom_item = itemBox.Item.nom_item,
                    seuil_min_item = itemBox.Item.seuil_min_item,
                    description_item = itemBox.Item.description_item,
                    id_img = itemBox.Item.id_img
                } : null,
                box = expand != null && expand.Contains("box") ? new ReadBoxDto
                {
                    id_box = itemBox.Box.id_box,
                    xstart_box = itemBox.Box.xstart_box,
                    ystart_box = itemBox.Box.ystart_box,
                    xend_box = itemBox.Box.xend_box,
                    yend_box = itemBox.Box.yend_box,
                    id_store = itemBox.Box.id_store
                } : null
            })
            .ToListAsync();
    }

    public async Task<int> GetItemsBoxsCountByItemId(int ItemId)
    {
        // check if the item exists
        if (!await _context.Items.AnyAsync(item => item.id_item == ItemId))
        {
            throw new KeyNotFoundException($"Item with id {ItemId} not found");
        }
        return await _context.ItemsBoxs
            .CountAsync(itemBox => itemBox.id_item == ItemId);
    }

    public async Task<ReadExtendedItemBoxDto> GetItemBoxById(int itemId, int boxId, List<string>? expand = null)
    {
        return await _context.ItemsBoxs
            .Where(itemBox => itemBox.id_box == boxId && itemBox.id_item == itemId)
            .Select(itemBox => new ReadExtendedItemBoxDto
            {
                id_box = itemBox.id_box,
                id_item = itemBox.id_item,
                qte_item_box = itemBox.qte_item_box,
                seuil_max_item_item_box = itemBox.seuil_max_item_item_box,
                item = expand != null && expand.Contains("item") ? new ReadItemDto
                {
                    id_item = itemBox.Item.id_item,
                    nom_item = itemBox.Item.nom_item,
                    seuil_min_item = itemBox.Item.seuil_min_item,
                    description_item = itemBox.Item.description_item,
                    id_img = itemBox.Item.id_img
                } : null,
                box = expand != null && expand.Contains("box") ? new ReadBoxDto
                {
                    id_box = itemBox.Box.id_box,
                    xstart_box = itemBox.Box.xstart_box,
                    ystart_box = itemBox.Box.ystart_box,
                    xend_box = itemBox.Box.xend_box,
                    yend_box = itemBox.Box.yend_box,
                    id_store = itemBox.Box.id_store
                } : null
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"ItemBox with id {itemId} and boxId {boxId} not found");
    }

    public async Task<ReadItemBoxDto> CreateItemBox(CreateItemBoxDto itemBoxDto)
    {
        // check if the box exists
        if (!await _context.Boxs.AnyAsync(box => box.id_box == itemBoxDto.id_box))
        {
            throw new KeyNotFoundException($"Box with id {itemBoxDto.id_box} not found");
        }
        // check if the item exists
        if (!await _context.Items.AnyAsync(item => item.id_item == itemBoxDto.id_item))
        {
            throw new KeyNotFoundException($"Item with id {itemBoxDto.id_item} not found");
        }
        // check if the item is already in the box
        if (await _context.ItemsBoxs.AnyAsync(itemBox => itemBox.id_box == itemBoxDto.id_box && itemBox.id_item == itemBoxDto.id_item))
        {
            throw new InvalidOperationException("Item is already in the box");
        }
        var newItemBox = new ItemsBoxs
        {
            id_box = itemBoxDto.id_box,
            id_item = itemBoxDto.id_item,
            qte_item_box = itemBoxDto.qte_item_box,
            seuil_max_item_item_box = itemBoxDto.seuil_max_item_item_box
        };
        _context.ItemsBoxs.Add(newItemBox);
        await _context.SaveChangesAsync();
        return new ReadItemBoxDto
        {
            id_box = newItemBox.id_box,
            id_item = newItemBox.id_item,
            qte_item_box = newItemBox.qte_item_box,
            seuil_max_item_item_box = newItemBox.seuil_max_item_item_box
        };
    }

    public async Task<ReadItemBoxDto> UpdateItemBox(int itemId, int boxId, UpdateItemBoxDto itemBoxDto)
    {
        var itemBoxToUpdate = await _context.ItemsBoxs.FindAsync(boxId, itemId) ?? throw new KeyNotFoundException($"ItemBox with id {itemId} and boxId {boxId} not found");
        if (itemBoxDto.qte_item_box is not null)
        {
            itemBoxToUpdate.qte_item_box = itemBoxDto.qte_item_box.Value;
        }

        if (itemBoxDto.seuil_max_item_item_box is not null)
        {
            itemBoxToUpdate.seuil_max_item_item_box = itemBoxDto.seuil_max_item_item_box.Value;
        }
        await _context.SaveChangesAsync();
        return new ReadItemBoxDto
        {
            id_box = itemBoxToUpdate.id_box,
            id_item = itemBoxToUpdate.id_item,
            qte_item_box = itemBoxToUpdate.qte_item_box,
            seuil_max_item_item_box = itemBoxToUpdate.seuil_max_item_item_box
        };
    }

    public async Task DeleteItemBox(int itemId, int boxId)
    {
        var itemBoxToDelete = await _context.ItemsBoxs.FindAsync(boxId, itemId) ?? throw new KeyNotFoundException($"ItemBox with id {itemId} and boxId {boxId} not found");
        _context.ItemsBoxs.Remove(itemBoxToDelete);
        await _context.SaveChangesAsync();
    }

    public async Task CheckIfStoreExists(int storeId, int boxId)
    {
        if (!await _context.Boxs.AnyAsync(box => box.id_box == boxId && box.id_store == storeId))
        {
            throw new KeyNotFoundException($"Box with id {boxId} not found in store with id {storeId}");
        }
    }
}