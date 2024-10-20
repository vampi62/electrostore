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

    public async Task<IEnumerable<ReadStoreTagDto>> GetStoresTagsByStoreId(int storeId, int limit = 100, int offset = 0)
    {
        // check if store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == storeId))
        {
            throw new KeyNotFoundException($"Store with id {storeId} not found");
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

    public async Task<IEnumerable<ReadStoreTagDto>> GetStoresTagsByTagId(int tagId, int limit = 100, int offset = 0)
    {
        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == tagId))
        {
            throw new KeyNotFoundException($"Tag with id {tagId} not found");
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

    public async Task<ReadStoreTagDto> GetStoreTagById(int storeId, int tagId)
    {
        var storeTag = await _context.StoresTags.FindAsync(storeId, tagId) ?? throw new KeyNotFoundException($"StoreTag with storeId {storeId} and tagId {tagId} not found");
        return new ReadStoreTagDto
        {
            id_store = storeTag.id_store,
            id_tag = storeTag.id_tag
        };
    }

    public async Task<IEnumerable<ReadStoreTagDto>> CreateStoreTags(int? storeId = null, int? tagId = null, int[]? tags = null, int[]? stores = null)
    {
        var newStoreTagList = new List<StoresTags>();
        if (storeId != null && tags != null)
        {
            // check if store exists
            if (!await _context.Stores.AnyAsync(s => s.id_store == storeId.Value))
            {
                throw new KeyNotFoundException($"Store with id {storeId.Value} not found");
            }
            // check if all tags exists
            for (int i = 0; i < tags.Length; i++)
            {
                if (!await _context.Tags.AnyAsync(t => t.id_tag == tags[i]))
                {
                    throw new KeyNotFoundException($"Tag with id {tags[i]} not found");
                }
            }
            // create store tag
            for (int i = 0; i < tags.Length; i++)
            {
                if (await _context.StoresTags.AnyAsync(st => st.id_store == storeId.Value && st.id_tag == tags[i]))
                {
                    throw new InvalidOperationException($"StoreTag with storeId {storeId.Value} and tagId {tags[i]} already exists");
                }
                var newStoreTag = new StoresTags
                {
                    id_store = storeId.Value,
                    id_tag = tags[i]
                };
                _context.StoresTags.Add(newStoreTag);
                newStoreTagList.Add(newStoreTag);
            }
            await _context.SaveChangesAsync();
        }
        else if (tagId != null && stores != null)
        {
            // check if tag exists
            if (!await _context.Tags.AnyAsync(t => t.id_tag == tagId.Value))
            {
                throw new KeyNotFoundException($"Tag with id {tagId.Value} not found");
            }
            // check if all stores exist
            for (int i = 0; i < stores.Length; i++)
            {
                if (!await _context.Stores.AnyAsync(s => s.id_store == stores[i]))
                {
                    throw new KeyNotFoundException($"Store with id {stores[i]} not found");
                }
            }
            // create the store tags
            for (int i = 0; i < stores.Length; i++)
            {
                if (await _context.StoresTags.AnyAsync(st => st.id_store == stores[i] && st.id_tag == tagId.Value))
                {
                    throw new InvalidOperationException($"StoreTag with storeId {stores[i]} and tagId {tagId.Value} already exists");
                }
                var newStoreTag = new StoresTags
                {
                    id_store = stores[i],
                    id_tag = tagId.Value
                };
                _context.StoresTags.Add(newStoreTag);
                newStoreTagList.Add(newStoreTag);
            }
            await _context.SaveChangesAsync();
        }
        else
        {
            throw new NotImplementedException("unknown parameters");
        }
        return newStoreTagList.Select(st => new ReadStoreTagDto
        {
            id_store = st.id_store,
            id_tag = st.id_tag
        });
    }

    public async Task<ReadStoreTagDto> CreateStoreTag(CreateStoreTagDto storeTagDto)
    {
        // check if store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == storeTagDto.id_store))
        {
            throw new KeyNotFoundException($"Store with id {storeTagDto.id_store} not found");
        }
        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == storeTagDto.id_tag))
        {
            throw new KeyNotFoundException($"Tag with id {storeTagDto.id_tag} not found");
        }
        // check if store tag already exists
        if (await _context.StoresTags.AnyAsync(st => st.id_store == storeTagDto.id_store && st.id_tag == storeTagDto.id_tag))
        {
            throw new InvalidOperationException($"StoreTag with storeId {storeTagDto.id_store} and tagId {storeTagDto.id_tag} already exists");
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
        var storeTag = await _context.StoresTags.FindAsync(storeId, tagId) ?? throw new KeyNotFoundException($"StoreTag with storeId {storeId} and tagId {tagId} not found");
        _context.StoresTags.Remove(storeTag);
        await _context.SaveChangesAsync();
    }
}