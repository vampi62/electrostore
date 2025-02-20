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

    public async Task<IEnumerable<ReadExtendedStoreTagDto>> GetStoresTagsByStoreId(int storeId, int limit = 100, int offset = 0, List<string>? expand = null)
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
            .Select(st => new ReadExtendedStoreTagDto
            {
                id_store = st.id_store,
                id_tag = st.id_tag,
                tag = expand != null && expand.Contains("tag") ? new ReadTagDto
                {
                    id_tag = st.Tag.id_tag,
                    nom_tag = st.Tag.nom_tag,
                    poids_tag = st.Tag.poids_tag
                } : null,
                store = expand != null && expand.Contains("store") ? new ReadStoreDto
                {
                    id_store = st.Store.id_store,
                    nom_store = st.Store.nom_store,
                    xlength_store = st.Store.xlength_store,
                    ylength_store = st.Store.ylength_store,
                    mqtt_name_store = st.Store.mqtt_name_store
                } : null
            })
            .ToListAsync();
    }

    public async Task<int> GetStoresTagsCountByStoreId(int storeId)
    {
        // check if store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == storeId))
        {
            throw new KeyNotFoundException($"Store with id {storeId} not found");
        }
        return await _context.StoresTags
            .CountAsync(st => st.id_store == storeId);
    }

    public async Task<IEnumerable<ReadExtendedStoreTagDto>> GetStoresTagsByTagId(int tagId, int limit = 100, int offset = 0, List<string>? expand = null)
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
            .Select(st => new ReadExtendedStoreTagDto
            {
                id_store = st.id_store,
                id_tag = st.id_tag,
                tag = expand != null && expand.Contains("tag") ? new ReadTagDto
                {
                    id_tag = st.Tag.id_tag,
                    nom_tag = st.Tag.nom_tag
                } : null,
                store = expand != null && expand.Contains("store") ? new ReadStoreDto
                {
                    id_store = st.Store.id_store,
                    nom_store = st.Store.nom_store,
                    xlength_store = st.Store.xlength_store,
                    ylength_store = st.Store.ylength_store,
                    mqtt_name_store = st.Store.mqtt_name_store
                } : null
            })
            .ToListAsync();
    }

    public async Task<int> GetStoresTagsCountByTagId(int tagId)
    {
        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == tagId))
        {
            throw new KeyNotFoundException($"Tag with id {tagId} not found");
        }
        return await _context.StoresTags
            .CountAsync(st => st.id_tag == tagId);
    }

    public async Task<ReadExtendedStoreTagDto> GetStoreTagById(int storeId, int tagId, List<string>? expand = null)
    {
        return await _context.StoresTags
            .Where(st => st.id_store == storeId && st.id_tag == tagId)
            .Select(st => new ReadExtendedStoreTagDto
            {
                id_store = st.id_store,
                id_tag = st.id_tag,
                tag = expand != null && expand.Contains("tag") ? new ReadTagDto
                {
                    id_tag = st.Tag.id_tag,
                    nom_tag = st.Tag.nom_tag
                } : null,
                store = expand != null && expand.Contains("store") ? new ReadStoreDto
                {
                    id_store = st.Store.id_store,
                    nom_store = st.Store.nom_store,
                    xlength_store = st.Store.xlength_store,
                    ylength_store = st.Store.ylength_store,
                    mqtt_name_store = st.Store.mqtt_name_store
                } : null
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"StoreTag with storeId {storeId} and tagId {tagId} not found");
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

    public async Task<ReadBulkStoreTagDto> CreateBulkStoreTag(List<CreateStoreTagDto> storeTagBulkDto)
    {
        var validQuery = new List<ReadStoreTagDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var storeTagDto in storeTagBulkDto)
        {
            try
            {
                validQuery.Add(await CreateStoreTag(storeTagDto));
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = storeTagDto
                });
            }
        }
        return new ReadBulkStoreTagDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }

    public async Task DeleteStoreTag(int storeId, int tagId)
    {
        var storeTag = await _context.StoresTags.FindAsync(storeId, tagId) ?? throw new KeyNotFoundException($"StoreTag with storeId {storeId} and tagId {tagId} not found");
        _context.StoresTags.Remove(storeTag);
        await _context.SaveChangesAsync();
    }

    public async Task<ReadBulkStoreTagDto> DeleteBulkItemTag(List<CreateStoreTagDto> storeTagBulkDto)
    {
        var validQuery = new List<ReadStoreTagDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var storeTagDto in storeTagBulkDto)
        {
            try
            {
                await DeleteStoreTag(storeTagDto.id_store, storeTagDto.id_tag);
                validQuery.Add(new ReadStoreTagDto
                {
                    id_store = storeTagDto.id_store,
                    id_tag = storeTagDto.id_tag
                });
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = storeTagDto
                });
            }
        }
        await _context.SaveChangesAsync();
        return new ReadBulkStoreTagDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }
}