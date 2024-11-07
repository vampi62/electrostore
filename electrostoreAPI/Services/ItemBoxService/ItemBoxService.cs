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

    public async Task<IEnumerable<ReadItemBoxDto>> GetItemsBoxsByBoxId(int boxId, int limit = 100, int offset = 0)
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
            .Select(itemBox => new ReadItemBoxDto
            {
                id_box = itemBox.id_box,
                id_item = itemBox.id_item,
                qte_item_box = itemBox.qte_item_box,
                seuil_max_item_item_box = itemBox.seuil_max_item_item_box
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<ReadItemBoxDto>> GetItemsBoxsByItemId(int ItemId, int limit = 100, int offset = 0)
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
            .Select(itemBox => new ReadItemBoxDto
            {
                id_box = itemBox.id_box,
                id_item = itemBox.id_item,
                qte_item_box = itemBox.qte_item_box,
                seuil_max_item_item_box = itemBox.seuil_max_item_item_box
            })
            .ToListAsync();
    }

    public async Task<ReadItemBoxDto> GetItemBoxById(int itemId, int boxId)
    {
        var itemBox = await _context.ItemsBoxs.FindAsync(boxId, itemId) ?? throw new KeyNotFoundException($"ItemBox with id {itemId} and boxId {boxId} not found");
        return new ReadItemBoxDto
        {
            id_box = itemBox.id_box,
            id_item = itemBox.id_item,
            qte_item_box = itemBox.qte_item_box,
            seuil_max_item_item_box = itemBox.seuil_max_item_item_box
        };
    }

    public async Task<ReadItemBoxDto> CreateItemBox(CreateItemBoxDto itemBoxDto)
    {
        if (itemBoxDto.qte_item_box < 0)
        {
            throw new ArgumentException("Quantity cannot be negative");
        }
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
        if (itemBoxDto.new_id_box != null)
        {
            // check if the new box exists
            if (!await _context.Boxs.AnyAsync(box => box.id_box == itemBoxDto.new_id_box))
            {
                throw new KeyNotFoundException($"Box with id {itemBoxDto.new_id_box} not found");
            }
            itemBoxToUpdate.id_box = itemBoxDto.new_id_box.Value;
        }

        if (itemBoxDto.qte_item_box != null)
        {
            if (itemBoxDto.qte_item_box < 0)
            {
                throw new ArgumentException("Quantity cannot be negative");
            }
            itemBoxToUpdate.qte_item_box = itemBoxDto.qte_item_box.Value;
        }

        if (itemBoxDto.seuil_max_item_item_box != null)
        {
            if (itemBoxDto.seuil_max_item_item_box < 0)
            {
                throw new ArgumentException("Seuil max cannot be negative");
            }
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

    public async Task CheckIfStoreExists(int storeId)
    {
        if (!await _context.Stores.AnyAsync(store => store.id_store == storeId))
        {
            throw new KeyNotFoundException($"Store with id {storeId} not found");
        }
    }
}