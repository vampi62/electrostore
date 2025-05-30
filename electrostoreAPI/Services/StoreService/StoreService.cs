using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using electrostore.Enums;
using electrostore.Services.SessionService;

namespace electrostore.Services.StoreService;

public class StoreService : IStoreService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;

    public StoreService(IMapper mapper, ApplicationDbContext context, ISessionService sessionService)
    {
        _mapper = mapper;
        _context = context;
        _sessionService = sessionService;
    }

    // limit the number of store to 100 and add offset and search parameters
    public async Task<IEnumerable<ReadExtendedStoreDto>> GetStores(int limit = 100, int offset = 0, List<string>? expand = null, List<int>? idResearch = null)
    {
        var query = _context.Stores.AsQueryable();
        if (idResearch is not null && idResearch.Count > 0)
        {
            query = query.Where(s => idResearch.Contains(s.id_store));
        }
        query = query.Skip(offset).Take(limit);
        var store = await query
            .OrderBy(s => s.id_store)
            .Select(s => new
            {
                Store = s,
                BoxsCount = s.Boxs.Count,
                LedsCount = s.Leds.Count,
                StoresTagsCount = s.StoresTags.Count,
                Boxs = expand != null && expand.Contains("boxs") ? s.Boxs.Take(20).ToList() : null,
                Leds = expand != null && expand.Contains("leds") ? s.Leds.Take(20).ToList() : null,
                StoresTags = expand != null && expand.Contains("stores_tags") ? s.StoresTags.Take(20).ToList() : null
            })
            .ToListAsync();
        return store.Select(s => {
            return _mapper.Map<ReadExtendedStoreDto>(s.Store) with
            {
                boxs_count = s.BoxsCount,
                leds_count = s.LedsCount,
                stores_tags_count = s.StoresTagsCount,
                boxs = _mapper.Map<IEnumerable<ReadBoxDto>>(s.Boxs),
                leds = _mapper.Map<IEnumerable<ReadLedDto>>(s.Leds),
                stores_tags = _mapper.Map<IEnumerable<ReadStoreTagDto>>(s.StoresTags)
            };
        }).ToList();
    }

    public async Task<int> GetStoresCount()
    {
        return await _context.Stores.CountAsync();
    }

    public async Task<ReadExtendedStoreDto> GetStoreById(int id, List<string>? expand = null)
    {
        var query = _context.Stores.AsQueryable();
        query = query.Where(s => s.id_store == id);
        var store = await query
            .Select(s => new
            {
                Store = s,
                BoxsCount = s.Boxs.Count,
                LedsCount = s.Leds.Count,
                StoresTagsCount = s.StoresTags.Count,
                Boxs = expand != null && expand.Contains("boxs") ? s.Boxs.Take(20).ToList() : null,
                Leds = expand != null && expand.Contains("leds") ? s.Leds.Take(20).ToList() : null,
                StoresTags = expand != null && expand.Contains("stores_tags") ? s.StoresTags.Take(20).ToList() : null
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Store with id {id} not found");
        return _mapper.Map<ReadExtendedStoreDto>(store.Store) with
        {
            boxs_count = store.BoxsCount,
            leds_count = store.LedsCount,
            stores_tags_count = store.StoresTagsCount,
            boxs = _mapper.Map<IEnumerable<ReadBoxDto>>(store.Boxs),
            leds = _mapper.Map<IEnumerable<ReadLedDto>>(store.Leds),
            stores_tags = _mapper.Map<IEnumerable<ReadStoreTagDto>>(store.StoresTags)
        };
    }

    public async Task<ReadStoreDto> CreateStore(CreateStoreDto storeDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You do not have permission to create a store");
        }
        var newStore = new Stores
        {
            nom_store = storeDto.nom_store,
            xlength_store = storeDto.xlength_store,
            ylength_store = storeDto.ylength_store,
            mqtt_name_store = storeDto.mqtt_name_store
        };
        _context.Stores.Add(newStore);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadStoreDto>(newStore);
    }

    public async Task<ReadStoreDto> UpdateStore(int id, UpdateStoreDto storeDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You do not have permission to update a store");
        }
        var storeToUpdate = await _context.Stores.FindAsync(id) ?? throw new KeyNotFoundException($"Store with id {id} not found");
        if (storeDto.nom_store is not null)
        {
            storeToUpdate.nom_store = storeDto.nom_store;
        }
        if (storeDto.xlength_store is not null)
        {
            // check if a box in the store is bigger than the new xlength_store
            if (await _context.Boxs.AnyAsync(b => b.id_store == id && b.xend_box > storeDto.xlength_store))
            {
                throw new ArgumentException("xlength_store is smaller than a box in the store");
            }
            storeToUpdate.xlength_store = storeDto.xlength_store.Value;
        }
        if (storeDto.ylength_store is not null)
        {
            // check if a box in the store is bigger than the new ylength_store
            if (await _context.Boxs.AnyAsync(b => b.id_store == id && b.yend_box > storeDto.ylength_store))
            {
                throw new ArgumentException("ylength_store is smaller than a box in the store");
            }
            storeToUpdate.ylength_store = storeDto.ylength_store.Value;
        }
        if (storeDto.mqtt_name_store is not null)
        {
            storeToUpdate.mqtt_name_store = storeDto.mqtt_name_store;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadStoreDto>(storeToUpdate);
    }

    public async Task DeleteStore(int id)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You do not have permission to delete a store");
        }
        var storeToDelete = await _context.Stores.FindAsync(id) ?? throw new KeyNotFoundException($"Store with id {id} not found");
        _context.Stores.Remove(storeToDelete);
        await _context.SaveChangesAsync();
    }
}