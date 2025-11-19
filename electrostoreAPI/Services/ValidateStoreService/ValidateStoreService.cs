using electrostore.Dto;
using electrostore.Models;
using Microsoft.EntityFrameworkCore;


namespace electrostore.Services.ValidateStoreService;

public class ValidateStoreService : IValidateStoreService
{
    private readonly ApplicationDbContext _context;

    public ValidateStoreService(ApplicationDbContext context)
    {
        _context = context;
    }

    public void ValidateLedPosition(Leds led, Stores store)
    {
        if (led.x_led >= store.xlength_store || led.y_led >= store.ylength_store)
        {
            throw new ArgumentException("Led position is out of store bounds");
        }
    }

    public void ValidateBoxPosition(Boxs box, Stores store)
    {
        if (box.xend_box <= box.xstart_box || box.yend_box <= box.ystart_box)
        {
            throw new ArgumentException($"Box start position ({box.xstart_box}, {box.ystart_box}) must be less than end position ({box.xend_box}, {box.yend_box}).");
        }
        if (box.xend_box > store.xlength_store || box.yend_box > store.ylength_store)
        {
            throw new ArgumentException($"Box position ({box.xstart_box}, {box.ystart_box}, {box.xend_box}, {box.yend_box}) is out of store bounds.");
        }
    }

    public async Task UpdateStoreInformations(Stores storeToUpdate, UpdateStoreDto storeDto)
    {
        if (storeDto.nom_store is not null)
        {
            storeToUpdate.nom_store = storeDto.nom_store;
        }
        if (storeDto.xlength_store is not null)
        {
            storeToUpdate.xlength_store = storeDto.xlength_store.Value;
        }
        if (storeDto.ylength_store is not null)
        {
            storeToUpdate.ylength_store = storeDto.ylength_store.Value;
        }
        if (storeDto.mqtt_name_store is not null)
        {
            storeToUpdate.mqtt_name_store = storeDto.mqtt_name_store;
        }
    }

    public async Task UpdateBoxInformations(Boxs boxToUpdate, UpdateBoxDto boxDto)
    {
        if (boxDto.xstart_box is not null)
        {
            boxToUpdate.xstart_box = boxDto.xstart_box.Value;
        }
        if (boxDto.ystart_box is not null)
        {
            boxToUpdate.ystart_box = boxDto.ystart_box.Value;
        }
        if (boxDto.yend_box is not null)
        {
            boxToUpdate.yend_box = boxDto.yend_box.Value;
        }
        if (boxDto.xend_box is not null)
        {
            boxToUpdate.xend_box = boxDto.xend_box.Value;
        }
        if (boxDto.new_id_store is not null)
        {
            if (!await _context.Stores.AnyAsync(s => s.id_store == boxDto.new_id_store))
            {
                throw new KeyNotFoundException($"Store with id '{boxDto.new_id_store}' not found");
            }
            boxToUpdate.id_store = boxDto.new_id_store.Value;
        }
    }

    public async Task UpdateLedInformations(Leds ledToUpdate, UpdateLedDto ledDto)
    {
        if (ledDto.x_led is not null)
        {
            ledToUpdate.x_led = ledDto.x_led.Value;
        }
        if (ledDto.y_led is not null)
        {
            ledToUpdate.y_led = ledDto.y_led.Value;
        }
        if (ledDto.mqtt_led_id is not null)
        {
            ledToUpdate.mqtt_led_id = ledDto.mqtt_led_id.Value;
        }
    }

    public async Task CheckUpdateStoreOutsideElement(Stores storeToUpdate)
    {
        // check if a box in the store is outside of the store size
        if (await _context.Boxs.AnyAsync(b => b.id_store == storeToUpdate.id_store && (b.xend_box > storeToUpdate.xlength_store || b.yend_box > storeToUpdate.ylength_store)))
        {
            throw new ArgumentException("you can't reduce the store size, a box will be out of store bounds");
        }
        // check if a led in the store is outside of the store size
        if (await _context.Leds.AnyAsync(l => l.id_store == storeToUpdate.id_store && (l.x_led > storeToUpdate.xlength_store || l.y_led > storeToUpdate.ylength_store)))
        {
            throw new ArgumentException("you can't reduce the store size, a led will be out of store bounds");
        }
    }

    public async Task CheckCreateBoxPositionOverlap(CreateBoxDto newBox)
    {
        // check if a box in the store has a XY position already taken
        // structure : (((NXS > OXS & NXS < OXE) | (NXE < OXE & NXE > OXS)) & ((NYS > OYS & NYS < OYE) | (NYE < OYE & NYE > OYS)))
        // N = new box, O = old box
        // X = x position, Y = y position, S = start, E = end
        if (await _context.Boxs.AnyAsync(b => b.id_store == newBox.id_store &&
            ((newBox.xstart_box <= b.xstart_box && newBox.xend_box > b.xstart_box) ||
            (newBox.xstart_box >= b.xstart_box && newBox.xstart_box < b.xend_box)) &&
            ((newBox.ystart_box <= b.ystart_box && newBox.yend_box > b.ystart_box) ||
            (newBox.ystart_box >= b.ystart_box && newBox.ystart_box < b.yend_box))))
        {
            throw new ArgumentException("Box XY position already taken");
        }
    }

    public async Task CheckUpdateBoxPositionOverlap(Boxs boxToUpdate)
    {
        // check if a box in the store has a XY position already taken except the box to update
        // structure : (((NXS > OXS & NXS < OXE) | (NXE < OXE & NXE > OXS)) & ((NYS > OYS & NYS < OYE) | (NYE < OYE & NYE > OYS)))
        // N = new box, O = old box
        // X = x position, Y = y position, S = start, E = end
        if (await _context.Boxs.AnyAsync(b => b.id_store == boxToUpdate.id_store && b.id_box != boxToUpdate.id_box &&
            ((boxToUpdate.xstart_box <= b.xstart_box && boxToUpdate.xend_box > b.xstart_box) ||
            (boxToUpdate.xstart_box >= b.xstart_box && boxToUpdate.xstart_box < b.xend_box)) &&
            ((boxToUpdate.ystart_box <= b.ystart_box && boxToUpdate.yend_box > b.ystart_box) ||
            (boxToUpdate.ystart_box >= b.ystart_box && boxToUpdate.ystart_box < b.yend_box))))
        {
            throw new ArgumentException("Box XY position already taken");
        }
    }
}