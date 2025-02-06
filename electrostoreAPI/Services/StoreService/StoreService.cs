using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.StoreService;

public class StoreService : IStoreService
{
    private readonly ApplicationDbContext _context;

    public StoreService(ApplicationDbContext context)
    {
        _context = context;
    }

    // limit the number of store to 100 and add offset and search parameters
    public async Task<IEnumerable<ReadExtendedStoreDto>> GetStores(int limit = 100, int offset = 0, List<string>? expand = null, List<int>? idResearch = null)
    {
        var query = _context.Stores.AsQueryable();
        if (idResearch != null)
        {
            query = query.Where(b => idResearch.Contains(b.id_store));
        }
        return await query
            .Skip(offset)
            .Take(limit)
            .Select(s => new ReadExtendedStoreDto
            {
                id_store = s.id_store,
                nom_store = s.nom_store,
                xlength_store = s.xlength_store,
                ylength_store = s.ylength_store,
                mqtt_name_store = s.mqtt_name_store,
                boxs = expand != null && expand.Contains("boxs") ? s.Boxs
                    .Select(b => new ReadBoxDto
                    {
                        id_box = b.id_box,
                        xstart_box = b.xstart_box,
                        ystart_box = b.ystart_box,
                        xend_box = b.xend_box,
                        yend_box = b.yend_box,
                        id_store = b.id_store
                    })
                    .ToArray() : null,
                leds = expand != null && expand.Contains("leds") ? s.Leds
                    .Select(l => new ReadLedDto
                    {
                        id_led = l.id_led,
                        x_led = l.x_led,
                        y_led = l.y_led,
                        mqtt_led_id = l.mqtt_led_id,
                        id_store = l.id_store
                    })
                    .ToArray() : null,
                stores_tags = expand != null && expand.Contains("stores_tags") ? s.StoresTags
                    .Select(t => new ReadStoreTagDto
                    {
                        id_store = t.id_store,
                        id_tag = t.id_tag
                    })
                    .ToArray() : null,
                boxs_count = s.Boxs.Count,
                leds_count = s.Leds.Count,
                stores_tags_count = s.StoresTags.Count
            }).ToListAsync();
    }

    public async Task<int> GetStoresCount()
    {
        return await _context.Stores.CountAsync();
    }

    public async Task<ReadExtendedStoreDto> GetStoreById(int id, List<string>? expand = null)
    {
        return await _context.Stores
            .Where(s => s.id_store == id)
            .Select(s => new ReadExtendedStoreDto
            {
                id_store = s.id_store,
                nom_store = s.nom_store,
                xlength_store = s.xlength_store,
                ylength_store = s.ylength_store,
                mqtt_name_store = s.mqtt_name_store,
                boxs = expand != null && expand.Contains("boxs") ? s.Boxs
                    .Select(b => new ReadBoxDto
                    {
                        id_box = b.id_box,
                        xstart_box = b.xstart_box,
                        ystart_box = b.ystart_box,
                        xend_box = b.xend_box,
                        yend_box = b.yend_box,
                        id_store = b.id_store
                    })
                    .ToArray() : null,
                leds = expand != null && expand.Contains("leds") ? s.Leds
                    .Select(l => new ReadLedDto
                    {
                        id_led = l.id_led,
                        x_led = l.x_led,
                        y_led = l.y_led,
                        mqtt_led_id = l.mqtt_led_id,
                        id_store = l.id_store
                    })
                    .ToArray() : null,
                stores_tags = expand != null && expand.Contains("stores_tags") ? s.StoresTags
                    .Select(t => new ReadStoreTagDto
                    {
                        id_store = t.id_store,
                        id_tag = t.id_tag
                    })
                    .ToArray() : null,
                boxs_count = s.Boxs.Count,
                leds_count = s.Leds.Count,
                stores_tags_count = s.StoresTags.Count
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Store with id {id} not found");
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
        return new ReadStoreDto
        {
            id_store = newStore.id_store,
            nom_store = newStore.nom_store,
            xlength_store = newStore.xlength_store,
            ylength_store = newStore.ylength_store,
            mqtt_name_store = newStore.mqtt_name_store
        };
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
        return new ReadStoreDto
        {
            id_store = storeToUpdate.id_store,
            nom_store = storeToUpdate.nom_store,
            xlength_store = storeToUpdate.xlength_store,
            ylength_store = storeToUpdate.ylength_store,
            mqtt_name_store = storeToUpdate.mqtt_name_store
        };
    }

    public async Task DeleteStore(int id)
    {
        var storeToDelete = await _context.Stores.FindAsync(id) ?? throw new KeyNotFoundException($"Store with id {id} not found");
        _context.Stores.Remove(storeToDelete);
        await _context.SaveChangesAsync();
    }
}