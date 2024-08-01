using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.ItemTagService;

public class ItemTagService : IItemTagService
{
    private readonly ApplicationDbContext _context;

    public ItemTagService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReadItemTagDto>> GetItemsTagsByItemId(int itemId, int limit = 100, int offset = 0)
    {
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            throw new ArgumentException("Item not found");
        }

        return await _context.ItemsTags
            .Skip(offset)
            .Take(limit)
            .Where(it => it.id_item == itemId)
            .Select(it => new ReadItemTagDto
            {
                id_item = it.id_item,
                id_tag = it.id_tag
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<ReadItemTagDto>> GetItemsTagsByTagId(int tagId, int limit = 100, int offset = 0)
    {
        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == tagId))
        {
            throw new ArgumentException("Tag not found");
        }

        return await _context.ItemsTags
            .Skip(offset)
            .Take(limit)
            .Where(it => it.id_tag == tagId)
            .Select(it => new ReadItemTagDto
            {
                id_item = it.id_item,
                id_tag = it.id_tag
            })
            .ToListAsync();
    }

    public async Task<ReadItemTagDto> GetItemTagById(int itemId, int tagId)
    {
        var itemTag = await _context.ItemsTags.FindAsync(itemId, tagId);
        if (itemTag == null)
        {
            throw new ArgumentException("ItemTag not found");
        }

        return new ReadItemTagDto
        {
            id_item = itemTag.id_item,
            id_tag = itemTag.id_tag
        };
    }

    public async Task<ReadItemTagDto> CreateItemTag(CreateItemTagDto itemTagDto)
    {
        // check if item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemTagDto.id_item))
        {
            throw new ArgumentException("Item not found");
        }

        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == itemTagDto.id_tag))
        {
            throw new ArgumentException("Tag not found");
        }

        // check if itemTag already exists
        if (await _context.ItemsTags.AnyAsync(it => it.id_item == itemTagDto.id_item && it.id_tag == itemTagDto.id_tag))
        {
            throw new ArgumentException("ItemTag already exists");
        }
        
        var itemTag = new ItemsTags
        {
            id_item = itemTagDto.id_item,
            id_tag = itemTagDto.id_tag
        };

        _context.ItemsTags.Add(itemTag);
        await _context.SaveChangesAsync();

        return new ReadItemTagDto
        {
            id_item = itemTag.id_item,
            id_tag = itemTag.id_tag
        };
    }

    public async Task DeleteItemTag(int itemId, int tagId)
    {
        var itemTagToDelete = await _context.ItemsTags.FindAsync(itemId, tagId);
        if (itemTagToDelete == null)
        {
            throw new ArgumentException("ItemTag not found");
        }

        _context.ItemsTags.Remove(itemTagToDelete);
        await _context.SaveChangesAsync();
    }
}