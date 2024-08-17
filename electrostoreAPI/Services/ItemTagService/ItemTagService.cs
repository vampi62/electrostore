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

    public async Task<ActionResult<IEnumerable<ReadItemTagDto>>> GetItemsTagsByItemId(int itemId, int limit = 100, int offset = 0)
    {
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_item = new string[] { "Item not found" } } });
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

    public async Task<ActionResult<IEnumerable<ReadItemTagDto>>> GetItemsTagsByTagId(int tagId, int limit = 100, int offset = 0)
    {
        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == tagId))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_tag = new string[] { "Tag not found" } } });
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

    public async Task<ActionResult<ReadItemTagDto>> GetItemTagById(int itemId, int tagId)
    {
        var itemTag = await _context.ItemsTags.FindAsync(itemId, tagId);
        if (itemTag == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_item = new string[] { "ItemTag not found" } } });
        }

        return new ReadItemTagDto
        {
            id_item = itemTag.id_item,
            id_tag = itemTag.id_tag
        };
    }

    public async Task<ActionResult<ReadItemTagDto>> CreateItemTag(CreateItemTagDto itemTagDto)
    {
        // check if item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemTagDto.id_item))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_item = new string[] { "Item not found" } } });
        }
        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == itemTagDto.id_tag))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_tag = new string[] { "Tag not found" } } });
        }
        // check if itemTag already exists
        if (await _context.ItemsTags.AnyAsync(it => it.id_item == itemTagDto.id_item && it.id_tag == itemTagDto.id_tag))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_item = new string[] { "ItemTag already exists" } } });
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

    public async Task<IActionResult> DeleteItemTag(int itemId, int tagId)
    {
        var itemTagToDelete = await _context.ItemsTags.FindAsync(itemId, tagId);
        if (itemTagToDelete == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_item = new string[] { "ItemTag not found" } } });
        }
        _context.ItemsTags.Remove(itemTagToDelete);
        await _context.SaveChangesAsync();
        return new OkResult();
    }
}