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

    public async Task<ActionResult<ReadStoreDto>> GetStoreById(int id)
    {
        var store = await _context.Stores.FindAsync(id);
        if (store == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_store = new string[] { "Store not found" } } });
        }

        return new ReadStoreDto
        {
            id_store = store.id_store,
            nom_store = store.nom_store,
            xlength_store = store.xlength_store,
            ylength_store = store.ylength_store,
            mqtt_name_store = store.mqtt_name_store
        };
    }

    public async Task<ActionResult<ReadStoreDto>> CreateStore(CreateStoreDto storeDto)
    {
        if (storeDto.xlength_store <= 0)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { xlength_store = new string[] { "xlength_store must be positive" } } });
        }
        if (storeDto.ylength_store <= 0)
        {
           return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { ylength_store = new string[] { "ylength_store must be positive" } } });
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

    public async Task<ActionResult<ReadStoreDto>> UpdateStore(int id, UpdateStoreDto storeDto)
    {
        var storeToUpdate = await _context.Stores.FindAsync(id);
        if (storeToUpdate == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_store = new string[] { "Store not found" } }});
        }

        if (storeDto.nom_store != null)
        {
            storeToUpdate.nom_store = storeDto.nom_store;
        }

        if (storeDto.xlength_store != null)
        {
            if (storeDto.xlength_store <= 0)
            {
                return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { xlength_store = new string[] { "xlength_store must be positive" } }});
            }
            // check if a box in the store is bigger than the new xlength_store
            if (await _context.Boxs.AnyAsync(b => b.id_store == id && b.xend_box > storeDto.xlength_store))
            {
                return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { xlength_store = new string[] { "xlength_store must be greater than the xend_box of the biggest box in the store" } }});
            }
            storeToUpdate.xlength_store = storeDto.xlength_store.Value;
        }
        if (storeDto.ylength_store != null)
        {
            if (storeDto.ylength_store <= 0)
            {
                return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { ylength_store = new string[] { "ylength_store must be positive" } }});
            }
            // check if a box in the store is bigger than the new ylength_store
            if (await _context.Boxs.AnyAsync(b => b.id_store == id && b.yend_box > storeDto.ylength_store))
            {
                return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { ylength_store = new string[] { "ylength_store must be greater than the yend_box of the biggest box in the store" } }});
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

    public async Task<IActionResult> DeleteStore(int id)
    {
        var storeToDelete = await _context.Stores.FindAsync(id);
        if (storeToDelete == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_store = new string[] { "Store not found" } }});
        }
        _context.Stores.Remove(storeToDelete);
        await _context.SaveChangesAsync();
        return new OkResult();
    }
}