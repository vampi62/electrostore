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
    public async Task<IEnumerable<ReadStoreDto>> GetStores(int limit = 100, int offset = 0)
    {
        return await _context.Stores
            .Skip(offset)
            .Take(limit)
            .Select(s => new ReadStoreDto
            {
                id_store = s.id_store,
                nom_store = s.nom_store,
                xlength_store = s.xlength_store,
                ylength_store = s.ylength_store,
                mqtt_name_store = s.mqtt_name_store
            }).ToListAsync();
    }

    public async Task<ReadStoreDto> GetStoreById(int id)
    {
        var store = await _context.Stores.FindAsync(id) ?? throw new KeyNotFoundException($"Store with id {id} not found");
        return new ReadStoreDto
        {
            id_store = store.id_store,
            nom_store = store.nom_store,
            xlength_store = store.xlength_store,
            ylength_store = store.ylength_store,
            mqtt_name_store = store.mqtt_name_store
        };
    }

    public async Task<ReadStoreDto> CreateStore(CreateStoreDto storeDto)
    {
        if (storeDto.xlength_store <= 0)
        {
            throw new ArgumentException("xlength_store must be greater than 0");
        }
        if (storeDto.ylength_store <= 0)
        {
            throw new ArgumentException("ylength_store must be greater than 0");
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
        if (storeDto.nom_store != null)
        {
            storeToUpdate.nom_store = storeDto.nom_store;
        }
        if (storeDto.xlength_store != null)
        {
            if (storeDto.xlength_store <= 0)
            {
                throw new ArgumentException("xlength_store must be greater than 0");
            }
            // check if a box in the store is bigger than the new xlength_store
            if (await _context.Boxs.AnyAsync(b => b.id_store == id && b.xend_box > storeDto.xlength_store))
            {
                throw new ArgumentException("xlength_store is smaller than a box in the store");
            }
            storeToUpdate.xlength_store = storeDto.xlength_store.Value;
        }
        if (storeDto.ylength_store != null)
        {
            if (storeDto.ylength_store <= 0)
            {
                throw new ArgumentException("ylength_store must be greater than 0");
            }
            // check if a box in the store is bigger than the new ylength_store
            if (await _context.Boxs.AnyAsync(b => b.id_store == id && b.yend_box > storeDto.ylength_store))
            {
                throw new ArgumentException("ylength_store is smaller than a box in the store");
            }
            storeToUpdate.ylength_store = storeDto.ylength_store.Value;
        }
        if (storeDto.mqtt_name_store != null)
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