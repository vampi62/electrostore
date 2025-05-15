using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.StoreService;

public class StoreService : IStoreService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public StoreService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    // limit the number of store to 100 and add offset and search parameters
    public async Task<IEnumerable<ReadExtendedStoreDto>> GetStores(int limit = 100, int offset = 0, List<string>? expand = null, List<int>? idResearch = null)
    {
        var query = _context.Stores.AsQueryable();
        if (idResearch is not null && idResearch.Count > 0)
        {
            query = query.Where(b => idResearch.Contains(b.id_store));
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("boxs"))
        {
            query = query.Include(s => s.Boxs);
        }
        if (expand != null && expand.Contains("leds"))
        {
            query = query.Include(s => s.Leds);
        }
        if (expand != null && expand.Contains("stores_tags"))
        {
            query = query.Include(s => s.StoresTags);
        }
        var store = await query.ToListAsync();
        return _mapper.Map<List<ReadExtendedStoreDto>>(store);
    }

    public async Task<int> GetStoresCount()
    {
        return await _context.Stores.CountAsync();
    }

    public async Task<ReadExtendedStoreDto> GetStoreById(int id, List<string>? expand = null)
    {
        var query = _context.Stores.AsQueryable();
        query = query.Where(s => s.id_store == id);
        if (expand != null && expand.Contains("boxs"))
        {
            query = query.Include(s => s.Boxs);
        }
        if (expand != null && expand.Contains("leds"))
        {
            query = query.Include(s => s.Leds);
        }
        if (expand != null && expand.Contains("stores_tags"))
        {
            query = query.Include(s => s.StoresTags);
        }
        var store = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Store with id {id} not found");
        return _mapper.Map<ReadExtendedStoreDto>(store);
    }

    public async Task<ReadStoreDto> CreateStore(CreateStoreDto storeDto)
    {
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
        var storeToDelete = await _context.Stores.FindAsync(id) ?? throw new KeyNotFoundException($"Store with id {id} not found");
        _context.Stores.Remove(storeToDelete);
        await _context.SaveChangesAsync();
    }
}