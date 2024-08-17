using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.StoreTagService;

public class StoreTagService : IStoreTagService
{
    private readonly ApplicationDbContext _context;

    public StoreTagService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ActionResult<IEnumerable<ReadStoreTagDto>>> GetStoresTagsByStoreId(int storeId, int limit = 100, int offset = 0)
    {
        // check if store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == storeId))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_store = new string[] { "Store not found" } } });
        }
        return await _context.StoresTags
            .Skip(offset)
            .Take(limit)
            .Where(st => st.id_store == storeId)
            .Select(st => new ReadStoreTagDto
            {
                id_store = st.id_store,
                id_tag = st.id_tag
            })
            .ToListAsync();
    }

    public async Task<ActionResult<IEnumerable<ReadStoreTagDto>>> GetStoresTagsByTagId(int tagId, int limit = 100, int offset = 0)
    {
        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == tagId))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_tag = new string[] { "Tag not found" } } });
        }
        return await _context.StoresTags
            .Skip(offset)
            .Take(limit)
            .Where(st => st.id_tag == tagId)
            .Select(st => new ReadStoreTagDto
            {
                id_store = st.id_store,
                id_tag = st.id_tag
            })
            .ToListAsync();
    }

    public async Task<ActionResult<ReadStoreTagDto>> GetStoreTagById(int storeId, int tagId)
    {
        var storeTag = await _context.StoresTags.FindAsync(storeId, tagId);
        if (storeTag == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_store = new string[] { "Store tag not found" } } });
        }

        return new ReadStoreTagDto
        {
            id_store = storeTag.id_store,
            id_tag = storeTag.id_tag
        };
    }

    public async Task<ActionResult<ReadStoreTagDto>> CreateStoreTag(CreateStoreTagDto storeTagDto)
    {
        // check if store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == storeTagDto.id_store))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_store = new string[] { "Store not found" } } });
        }

        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == storeTagDto.id_tag))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_tag = new string[] { "Tag not found" } } });
        }

        // check if store tag already exists
        if (await _context.StoresTags.AnyAsync(st => st.id_store == storeTagDto.id_store && st.id_tag == storeTagDto.id_tag))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_store = new string[] { "Store tag already exists" } } });
        }

        var newStoreTag = new StoresTags
        {
            id_store = storeTagDto.id_store,
            id_tag = storeTagDto.id_tag
        };

        _context.StoresTags.Add(newStoreTag);
        await _context.SaveChangesAsync();

        return new ReadStoreTagDto
        {
            id_store = newStoreTag.id_store,
            id_tag = newStoreTag.id_tag
        };
    }

    public async Task<IActionResult> DeleteStoreTag(int storeId, int tagId)
    {
        var storeTag = await _context.StoresTags.FindAsync(storeId, tagId);
        if (storeTag == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_store = new string[] { "Store tag not found" } } });
        }

        _context.StoresTags.Remove(storeTag);
        await _context.SaveChangesAsync();
        return new OkResult();
    }
}