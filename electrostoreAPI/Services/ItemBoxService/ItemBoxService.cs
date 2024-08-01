using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

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
            throw new ArgumentException("Box not found");
        }

        return await _context.ItemsBoxs
            .Skip(offset)
            .Take(limit)
            .Where(itemBox => itemBox.id_box == boxId)
            .Select(itemBox => new ReadItemBoxDto
            {
                id_box = itemBox.id_box,
                id_item = itemBox.id_item,
                qte_itembox = itemBox.qte_itembox,
                seuil_max_itemitembox = itemBox.seuil_max_itemitembox
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<ReadItemBoxDto>> GetItemsBoxsByItemId(int ItemId, int limit = 100, int offset = 0)
    {
        // check if the item exists
        if (!await _context.Items.AnyAsync(item => item.id_item == ItemId))
        {
            throw new ArgumentException("Item not found");
        }

        return await _context.ItemsBoxs
            .Skip(offset)
            .Take(limit)
            .Where(itemBox => itemBox.id_item == ItemId)
            .Select(itemBox => new ReadItemBoxDto
            {
                id_box = itemBox.id_box,
                id_item = itemBox.id_item,
                qte_itembox = itemBox.qte_itembox,
                seuil_max_itemitembox = itemBox.seuil_max_itemitembox
            })
            .ToListAsync();
    }

    public async Task<ReadItemBoxDto> GetItemBoxById(int itemId, int boxId)
    {
        var itemBox = await _context.ItemsBoxs.FindAsync(boxId, itemId);
        if (itemBox == null)
        {
            throw new ArgumentException("ItemBox not found");
        }

        return new ReadItemBoxDto
        {
            id_box = itemBox.id_box,
            id_item = itemBox.id_item,
            qte_itembox = itemBox.qte_itembox,
            seuil_max_itemitembox = itemBox.seuil_max_itemitembox
        };
    }

    public async Task<ReadItemBoxDto> CreateItemBox(CreateItemBoxDto itemBoxDto)
    {
        if (itemBoxDto.qte_itembox < 0)
        {
            throw new ArgumentException("qte_itembox must be greater than 0");
        }

        // check if the box exists
        if (!await _context.Boxs.AnyAsync(box => box.id_box == itemBoxDto.id_box))
        {
            throw new ArgumentException("Box not found");
        }

        // check if the item exists
        if (!await _context.Items.AnyAsync(item => item.id_item == itemBoxDto.id_item))
        {
            throw new ArgumentException("Item not found");
        }

        // check if the item is already in the box
        if (await _context.ItemsBoxs.AnyAsync(itemBox => itemBox.id_box == itemBoxDto.id_box && itemBox.id_item == itemBoxDto.id_item))
        {
            throw new ArgumentException("ItemBox already exists");
        }

        var newItemBox = new ItemsBoxs
        {
            id_box = itemBoxDto.id_box,
            id_item = itemBoxDto.id_item,
            qte_itembox = itemBoxDto.qte_itembox,
            seuil_max_itemitembox = itemBoxDto.seuil_max_itemitembox
        };

        _context.ItemsBoxs.Add(newItemBox);
        await _context.SaveChangesAsync();

        return new ReadItemBoxDto
        {
            id_box = newItemBox.id_box,
            id_item = newItemBox.id_item,
            qte_itembox = newItemBox.qte_itembox,
            seuil_max_itemitembox = newItemBox.seuil_max_itemitembox
        };
    }

    public async Task<ReadItemBoxDto> UpdateItemBox(int itemId, int boxId, UpdateItemBoxDto itemBoxDto)
    {
        var itemBoxToUpdate = await _context.ItemsBoxs.FindAsync(boxId, itemId);
        if (itemBoxToUpdate == null)
        {
            throw new ArgumentException("ItemBox not found");
        }

        if (itemBoxDto.new_id_box != null)
        {
            // check if the new box exists
            if (!await _context.Boxs.AnyAsync(box => box.id_box == itemBoxDto.new_id_box))
            {
                throw new ArgumentException("Box not found");
            }
            itemBoxToUpdate.id_box = itemBoxDto.new_id_box.Value;
        }

        if (itemBoxDto.qte_itembox != null)
        {
            if (itemBoxDto.qte_itembox < 0)
            {
                throw new ArgumentException("qte_itembox must be greater than 0");
            }
            itemBoxToUpdate.qte_itembox = itemBoxDto.qte_itembox.Value;
        }

        if (itemBoxDto.seuil_max_itemitembox != null)
        {
            if (itemBoxDto.seuil_max_itemitembox < 0)
            {
                throw new ArgumentException("seuil_max_itemitembox must be greater than 0");
            }
            itemBoxToUpdate.seuil_max_itemitembox = itemBoxDto.seuil_max_itemitembox.Value;
        }

        await _context.SaveChangesAsync();

        return new ReadItemBoxDto
        {
            id_box = itemBoxToUpdate.id_box,
            id_item = itemBoxToUpdate.id_item,
            qte_itembox = itemBoxToUpdate.qte_itembox,
            seuil_max_itemitembox = itemBoxToUpdate.seuil_max_itemitembox
        };
    }

    public async Task DeleteItemBox(int itemId, int boxId)
    {
        var itemBoxToDelete = await _context.ItemsBoxs.FindAsync(boxId, itemId);
        if (itemBoxToDelete == null)
        {
            throw new ArgumentException("ItemBox not found");
        }

        _context.ItemsBoxs.Remove(itemBoxToDelete);
        await _context.SaveChangesAsync();
    }
}