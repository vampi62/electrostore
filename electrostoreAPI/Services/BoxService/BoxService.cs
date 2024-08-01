using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.BoxService;

public class BoxService : IBoxService
{
    private readonly ApplicationDbContext _context;

    public BoxService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReadBoxDto>> GetBoxs(int limit = 100, int offset = 0)
    {
        return await _context.Boxs
            .Skip(offset)
            .Take(limit)
            .Select(s => new ReadBoxDto
            {
                id_box = s.id_box,
                xstart_box = s.xstart_box,
                ystart_box = s.ystart_box,
                xend_box = s.xend_box,
                yend_box = s.yend_box,
                id_store = s.id_store
            }).ToListAsync();
    }

    public async Task<IEnumerable<ReadBoxDto>> GetBoxsByStoreId(int storeId, int limit = 100, int offset = 0)
    {
        // check if the store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == storeId))
        {
            throw new ArgumentException("Store not found");
        }

        return await _context.Boxs
            .Where(b => b.id_store == storeId)
            .Skip(offset)
            .Take(limit)
            .Select(s => new ReadBoxDto
            {
                id_box = s.id_box,
                xstart_box = s.xstart_box,
                ystart_box = s.ystart_box,
                xend_box = s.xend_box,
                yend_box = s.yend_box,
                id_store = s.id_store
            }).ToListAsync();
    }

    public async Task<ReadBoxDto> GetBoxById(int id, int? storeId = null)
    {
        var box = await _context.Boxs.FindAsync(id);
        if (box == null)
        {
            throw new ArgumentException("Box not found");
        }
        if (storeId != null && box.id_store != storeId)
        {
            throw new ArgumentException("Box not found");
        }

        return new ReadBoxDto
        {
            id_box = box.id_box,
            xstart_box = box.xstart_box,
            ystart_box = box.ystart_box,
            yend_box = box.yend_box,
            xend_box = box.xend_box,
            id_store = box.id_store
        };
    }

    public async Task<ReadBoxDto> CreateBox(CreateBoxDto boxDto)
    {
        // check if all the values are greater than 0
        if (boxDto.xstart_box < 0 || boxDto.ystart_box < 0 || boxDto.yend_box < 0 || boxDto.xend_box < 0)
        {
            throw new ArgumentException("All values must be greater than 0");
        }
        // end position must be greater than start position
        if (boxDto.xend_box <= boxDto.xstart_box || boxDto.yend_box <= boxDto.ystart_box)
        {
            throw new ArgumentException("End position must be greater than start position");
        }

        // check if the store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == boxDto.id_store))
        {
            throw new ArgumentException("Store not found");
        }

        // check if a box in the same store has a XY position already taken
        // (((NXS > OXS && NXS < OXE) || (NXE < OXE && NXE > OXS)) && ((NYS > OYS && NYS < OYE) || (NYE < OYE && NYE > OYS)))
        // N = new box, O = old box
        // X = x position, Y = y position, S = start, E = end
        if (await _context.Boxs.AnyAsync(b => b.id_store == boxDto.id_store &&
                                              ((boxDto.xstart_box > b.xstart_box && boxDto.xstart_box < b.xend_box) ||
                                               (boxDto.xend_box < b.xend_box && boxDto.xend_box > b.xstart_box)) &&
                                              ((boxDto.ystart_box > b.ystart_box && boxDto.ystart_box < b.yend_box) ||
                                               (boxDto.yend_box < b.yend_box && boxDto.yend_box > b.ystart_box))))
        {
            throw new ArgumentException("XY position already taken");
        }

        var newBox = new Boxs
        {
            xstart_box = boxDto.xstart_box,
            ystart_box = boxDto.ystart_box,
            yend_box = boxDto.yend_box,
            xend_box = boxDto.xend_box,
            id_store = boxDto.id_store
        };

        _context.Boxs.Add(newBox);
        await _context.SaveChangesAsync();

        return new ReadBoxDto
        {
            id_box = newBox.id_box,
            xstart_box = newBox.xstart_box,
            ystart_box = newBox.ystart_box,
            yend_box = newBox.yend_box,
            xend_box = newBox.xend_box,
            id_store = newBox.id_store
        };
    }

    public async Task<ReadBoxDto> UpdateBox(int id, UpdateBoxDto boxDto, int? storeId = null)
    {
        var boxToUpdate = await _context.Boxs.FindAsync(id);

        bool newXYPosition = false;
        if (boxToUpdate == null)
        {
            throw new ArgumentException("Box not found");
        }
        if (storeId != null && boxToUpdate.id_store != storeId)
        {
            throw new ArgumentException("Box not found");
        }

        if (boxDto.xstart_box != null)
        {
            if (boxDto.xstart_box < 0)
            {
                throw new ArgumentException("xstart_box must be greater than 0");
            }
            boxToUpdate.xstart_box = boxDto.xstart_box.Value;
            newXYPosition = true;
        }

        if (boxDto.ystart_box != null)
        {
            if (boxDto.ystart_box < 0)
            {
                throw new ArgumentException("ystart_box must be greater than 0");
            }
            boxToUpdate.ystart_box = boxDto.ystart_box.Value;
            newXYPosition = true;
        }

        if (boxDto.yend_box != null)
        {
            if (boxDto.yend_box < 0)
            {
                throw new ArgumentException("yend_box must be greater than 0");
            }
            boxToUpdate.yend_box = boxDto.yend_box.Value;
            newXYPosition = true;
        }

        if (boxDto.xend_box != null)
        {
            if (boxDto.xend_box < 0)
            {
                throw new ArgumentException("xend_box must be greater than 0");
            }
            boxToUpdate.xend_box = boxDto.xend_box.Value;
            newXYPosition = true;
        }
        
        // end position must be greater than start position
        if (boxToUpdate.xend_box <= boxToUpdate.xstart_box || boxToUpdate.yend_box <= boxToUpdate.ystart_box)
        {
            throw new ArgumentException("End position must be greater than start position");
        }

        if (boxDto.new_id_store != null)
        {
            if (!await _context.Stores.AnyAsync(s => s.id_store == boxDto.new_id_store))
            {
                throw new ArgumentException("Store not found");
            }
            boxToUpdate.id_store = boxDto.new_id_store.Value;
            newXYPosition = true;
        }

        if (newXYPosition)
        {
            // check if a box in the store has a XY position already taken except the box to update
            // (((NXS > OXS && NXS < OXE) || (NXE < OXE && NXE > OXS)) && ((NYS > OYS && NYS < OYE) || (NYE < OYE && NYE > OYS)))
            // N = new box, O = old box
            // X = x position, Y = y position, S = start, E = end
            if (await _context.Boxs.AnyAsync(b => b.id_store == boxToUpdate.id_store && b.id_box != boxToUpdate.id_box &&
                                                  ((boxToUpdate.xstart_box > b.xstart_box && boxToUpdate.xstart_box < b.xend_box) ||
                                                   (boxToUpdate.xend_box < b.xend_box && boxToUpdate.xend_box > b.xstart_box)) &&
                                                  ((boxToUpdate.ystart_box > b.ystart_box && boxToUpdate.ystart_box < b.yend_box) ||
                                                   (boxToUpdate.yend_box < b.yend_box && boxToUpdate.yend_box > b.ystart_box))))
            {
                throw new ArgumentException("XY position already taken");
            }
        }

        await _context.SaveChangesAsync();

        return new ReadBoxDto
        {
            id_box = boxToUpdate.id_box,
            xstart_box = boxToUpdate.xstart_box,
            ystart_box = boxToUpdate.ystart_box,
            yend_box = boxToUpdate.yend_box,
            xend_box = boxToUpdate.xend_box,
            id_store = boxToUpdate.id_store
        };
    }

    public async Task DeleteBox(int id, int? storeId = null)
    {
        var boxToDelete = await _context.Boxs.FindAsync(id);
        if (boxToDelete == null)
        {
            throw new ArgumentException("Box not found");
        }
        if (storeId != null && boxToDelete.id_store != storeId)
        {
            throw new ArgumentException("Box not found");
        }

        _context.Boxs.Remove(boxToDelete);
        await _context.SaveChangesAsync();

    }
}