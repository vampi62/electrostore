using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.StoreTagService;

public class StoreTagService : IStoreTagService
{
    private readonly ApplicationDbContext _context;

    public StoreTagService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReadStoreTagDto>> GetStoresTagsByStoreId(int storeId, int limit = 100, int offset = 0)
    {
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

    public async Task<IEnumerable<ReadStoreTagDto>> GetStoresTagsByTagId(int tagId, int limit = 100, int offset = 0)

    {
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

    public async Task<ReadStoreTagDto> GetStoreTagById(int storeId, int tagId)
    {
        var storeTag = await _context.StoresTags.FindAsync(storeId, tagId);
        if (storeTag == null)
        {
            throw new ArgumentException("Store tag not found");
        }

        return new ReadStoreTagDto
        {
            id_store = storeTag.id_store,
            id_tag = storeTag.id_tag
        };
    }

    public async Task<ReadStoreTagDto> CreateStoreTag(CreateStoreTagDto storeTagDto)
    {
        // check if store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == storeTagDto.id_store))
        {
            throw new ArgumentException("Store not found");
        }

        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == storeTagDto.id_tag))
        {
            throw new ArgumentException("Tag not found");
        }

        // check if store tag already exists
        if (await _context.StoresTags.AnyAsync(st => st.id_store == storeTagDto.id_store && st.id_tag == storeTagDto.id_tag))
        {
            throw new ArgumentException("Store tag already exists");
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

    public async Task DeleteStoreTag(int storeId, int tagId)
    {
        var storeTag = await _context.StoresTags.FindAsync(storeId, tagId);
        if (storeTag == null)
        {
            throw new ArgumentException("Store tag not found");
        }

        _context.StoresTags.Remove(storeTag);
        await _context.SaveChangesAsync();
    }
}