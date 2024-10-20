using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Microsoft.AspNetCore.Mvc;

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
            throw new KeyNotFoundException($"Item with id {itemId} not found");
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
            throw new KeyNotFoundException($"Tag with id {tagId} not found");
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
        var itemTag = await _context.ItemsTags.FindAsync(itemId, tagId) ?? throw new KeyNotFoundException($"ItemTag with id_item {itemId} and id_tag {tagId} not found");
        return new ReadItemTagDto
        {
            id_item = itemTag.id_item,
            id_tag = itemTag.id_tag
        };
    }

    public async Task<IEnumerable<ReadItemTagDto>> CreateItemTags(int? itemId = null, int? tagId = null, int[]? tags = null, int[]? items = null)
    {
        var newItemTagList = new List<ItemsTags>();
        if (itemId != null && tags != null)
        {
            // check if item exists
            if (!await _context.Items.AnyAsync(i => i.id_item == itemId.Value))
            {
                throw new KeyNotFoundException($"Item with id {itemId} not found");
            }
            // check if tags exist
            for (int i = 0; i < tags.Length; i++)
            {
                if (!await _context.Tags.AnyAsync(t => t.id_tag == tags[i]))
                {
                    throw new KeyNotFoundException($"Tag with id {tags[i]} not found");
                }
            }
            // create the itemTags
            for (int i = 0; i < tags.Length; i++)
            {
                if (await _context.ItemsTags.AnyAsync(it => it.id_item == itemId.Value && it.id_tag == tags[i]))
                {
                    throw new InvalidOperationException($"ItemTag with id_item {itemId} and id_tag {tags[i]} already exists");
                }
                var itemTag = new ItemsTags
                {
                    id_item = itemId.Value,
                    id_tag = tags[i]
                };
                _context.ItemsTags.Add(itemTag);
                newItemTagList.Add(itemTag);
            }
            await _context.SaveChangesAsync();
        }
        else if (tagId != null && items != null)
        {
            // check if tag exists
            if (!await _context.Tags.AnyAsync(t => t.id_tag == tagId.Value))
            {
                throw new KeyNotFoundException($"Tag with id {tagId} not found");
            }
            // check if all items exist
            for (int i = 0; i < items.Length; i++)
            {
                if (!await _context.Items.AnyAsync(it => it.id_item == items[i]))
                {
                    throw new KeyNotFoundException($"Item with id {items[i]} not found");
                }
            }
            // create the itemTags
            for (int i = 0; i < items.Length; i++)
            {
                if (await _context.ItemsTags.AnyAsync(it => it.id_item == items[i] && it.id_tag == tagId.Value))
                {
                    throw new InvalidOperationException($"ItemTag with id_item {items[i]} and id_tag {tagId} already exists");
                }
                var itemTag = new ItemsTags
                {
                    id_item = items[i],
                    id_tag = tagId.Value
                };
                _context.ItemsTags.Add(itemTag);
                newItemTagList.Add(itemTag);
            }
            await _context.SaveChangesAsync();
        }
        else
        {
            throw new NotImplementedException();
        }
        return newItemTagList.Select(it => new ReadItemTagDto
        {
            id_item = it.id_item,
            id_tag = it.id_tag
        });
    }

    public async Task<ReadItemTagDto> CreateItemTag(CreateItemTagDto itemTagDto)
    {
        // check if item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemTagDto.id_item))
        {
            throw new KeyNotFoundException($"Item with id {itemTagDto.id_item} not found");
        }
        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == itemTagDto.id_tag))
        {
            throw new KeyNotFoundException($"Tag with id {itemTagDto.id_tag} not found");
        }
        // check if itemTag already exists
        if (await _context.ItemsTags.AnyAsync(it => it.id_item == itemTagDto.id_item && it.id_tag == itemTagDto.id_tag))
        {
            throw new InvalidOperationException($"ItemTag with id_item {itemTagDto.id_item} and id_tag {itemTagDto.id_tag} already exists");
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
        var itemTagToDelete = await _context.ItemsTags.FindAsync(itemId, tagId) ?? throw new KeyNotFoundException($"ItemTag with id_item {itemId} and id_tag {tagId} not found");
        _context.ItemsTags.Remove(itemTagToDelete);
        await _context.SaveChangesAsync();
    }
}