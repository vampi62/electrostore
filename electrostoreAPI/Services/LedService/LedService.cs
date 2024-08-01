using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.LedService;

public class LedService : ILedService
{
    private readonly ApplicationDbContext _context;

    public LedService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReadLedDto>> GetLeds(int limit = 100, int offset = 0)
    {
        return await _context.Leds
            .Skip(offset)
            .Take(limit)
            .Select(led => new ReadLedDto
            {
                id_led = led.id_led,
                x_led = led.x_led,
                y_led = led.y_led,
                id_store = led.id_store,
                mqtt_led_id = led.mqtt_led_id
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<ReadLedDto>> GetLedsByStoreId(int storeId, int limit = 100, int offset = 0)
    {
        return await _context.Leds
            .Where(led => led.id_store == storeId)
            .Skip(offset)
            .Take(limit)
            .Select(led => new ReadLedDto
            {
                id_led = led.id_led,
                x_led = led.x_led,
                y_led = led.y_led,
                id_store = led.id_store,
                mqtt_led_id = led.mqtt_led_id
            })
            .ToListAsync();
    }

    public async Task<ReadLedDto> GetLedById(int id, int? storeId = null)
    {
        var led = await _context.Leds.FindAsync(id);
        if (led == null)
        {
            throw new ArgumentException("Led not found");
        }
        if (led.id_store != storeId)
        {
            throw new ArgumentException("Led does not belong to this store");
        }

        return new ReadLedDto
        {
            id_led = led.id_led,
            x_led = led.x_led,
            y_led = led.y_led,
            id_store = led.id_store,
            mqtt_led_id = led.mqtt_led_id
        };
    }

    public async Task<ReadLedDto> CreateLed(CreateLedDto ledDto)
    {
        if (ledDto.x_led < 0 || ledDto.y_led < 0 || ledDto.mqtt_led_id < 0)
        {
            throw new ArgumentException("Coordinates must be positive");
        }

        // check if store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == ledDto.id_store))
        {
            throw new ArgumentException("Store not found");
        }

        var newLed = new Leds
        {
            x_led = ledDto.x_led,
            y_led = ledDto.y_led,
            id_store = ledDto.id_store,
            mqtt_led_id = ledDto.mqtt_led_id
        };

        _context.Leds.Add(newLed);
        await _context.SaveChangesAsync();

        return new ReadLedDto
        {
            id_led = newLed.id_led,
            x_led = newLed.x_led,
            y_led = newLed.y_led,
            id_store = newLed.id_store,
            mqtt_led_id = newLed.mqtt_led_id
        };
    }

    public async Task<ReadLedDto> UpdateLed(int id, UpdateLedDto ledDto, int? storeId = null)
    {
        var ledToUpdate = await _context.Leds.FindAsync(id);
        if (ledToUpdate == null)
        {
            throw new ArgumentException("Led not found");
        }
        if (ledToUpdate.id_store != storeId)
        {
            throw new ArgumentException("Led does not belong to this store");
        }

        if (ledDto.x_led != null)
        {
            if (ledDto.x_led < 0)
            {
                throw new ArgumentException("Coordinates must be positive");
            }
            ledToUpdate.x_led = ledDto.x_led.Value;
        }

        if (ledDto.y_led != null)
        {
            if (ledDto.y_led < 0)
            {
                throw new ArgumentException("Coordinates must be positive");
            }
            ledToUpdate.y_led = ledDto.y_led.Value;
        }

        if (ledDto.new_id_store != null)
        {
            // check if store exists
            if (!await _context.Stores.AnyAsync(s => s.id_store == ledDto.new_id_store))
            {
                throw new ArgumentException("Store not found");
            }
            ledToUpdate.id_store = ledDto.new_id_store.Value;
        }

        if (ledDto.mqtt_led_id != null)
        {
            if (ledDto.mqtt_led_id < 0)
            {
                throw new ArgumentException("Mqtt id must be positive");
            }
            ledToUpdate.mqtt_led_id = ledDto.mqtt_led_id.Value;
        }

        await _context.SaveChangesAsync();

        return new ReadLedDto
        {
            id_led = ledToUpdate.id_led,
            x_led = ledToUpdate.x_led,
            y_led = ledToUpdate.y_led,
            id_store = ledToUpdate.id_store,
            mqtt_led_id = ledToUpdate.mqtt_led_id
        };
    }

    public async Task DeleteLed(int id, int? storeId = null)
    {
        var ledToDelete = await _context.Leds.FindAsync(id);
        if (ledToDelete == null)
        {
            throw new ArgumentException("Led not found");
        }
        if (ledToDelete.id_store != storeId)
        {
            throw new ArgumentException("Led does not belong to this store");
        }

        _context.Leds.Remove(ledToDelete);
        await _context.SaveChangesAsync();
    }
}