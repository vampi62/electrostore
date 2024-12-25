using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.BoxService;

public class BoxService : IBoxService
{
    private readonly ApplicationDbContext _context;

    public BoxService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReadExtendedBoxDto>> GetBoxsByStoreId(int storeId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if the store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == storeId))
        {
            throw new KeyNotFoundException($"Store with id {storeId} not found");
        }
        return await _context.Boxs
            .Where(b => b.id_store == storeId)
            .Skip(offset)
            .Take(limit)
            .Select(s => new ReadExtendedBoxDto
            {
                id_box = s.id_box,
                xstart_box = s.xstart_box,
                ystart_box = s.ystart_box,
                xend_box = s.xend_box,
                yend_box = s.yend_box,
                id_store = s.id_store,
                store = expand != null && expand.Contains("store") ? new ReadStoreDto
                {
                    id_store = s.Store.id_store,
                    nom_store = s.Store.nom_store,
                    xlength_store = s.Store.xlength_store,
                    ylength_store = s.Store.ylength_store,
                    mqtt_name_store = s.Store.mqtt_name_store
                } : null,
                box_tags = expand != null && expand.Contains("box_tags") ? s.BoxsTags
                    .Select(bt => new ReadBoxTagDto
                    {
                        id_box = bt.id_box,
                        id_tag = bt.id_tag
                    }).ToArray() : null,
                item_boxs = expand != null && expand.Contains("item_boxs") ? s.ItemsBoxs
                    .Select(ib => new ReadItemBoxDto
                    {
                        id_box = ib.id_box,
                        id_item = ib.id_item,
                        qte_item_box = ib.qte_item_box,
                        seuil_max_item_item_box = ib.seuil_max_item_item_box
                    }).ToArray() : null,
                box_tags_count = s.BoxsTags.Count,
                item_boxs_count = s.ItemsBoxs.Count
            }).ToListAsync();
    }

    public async Task<int> GetBoxsCountByStoreId(int storeId)
    {
        // check if the store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == storeId))
        {
            throw new KeyNotFoundException($"Store with id {storeId} not found");
        }
        return await _context.Boxs
            .Where(b => b.id_store == storeId)
            .CountAsync();
    }

    public async Task<ReadExtendedBoxDto> GetBoxById(int id, int? storeId = null, List<string>? expand = null)
    {
        return await _context.Boxs
            .Where(b => b.id_box == id && (storeId == null || b.id_store == storeId))
            .Select(s => new ReadExtendedBoxDto
            {
                id_box = s.id_box,
                xstart_box = s.xstart_box,
                ystart_box = s.ystart_box,
                xend_box = s.xend_box,
                yend_box = s.yend_box,
                id_store = s.id_store,
                store = expand != null && expand.Contains("store") ? new ReadStoreDto
                {
                    id_store = s.Store.id_store,
                    nom_store = s.Store.nom_store,
                    xlength_store = s.Store.xlength_store,
                    ylength_store = s.Store.ylength_store,
                    mqtt_name_store = s.Store.mqtt_name_store
                } : null,
                box_tags = expand != null && expand.Contains("box_tags") ? s.BoxsTags
                    .Select(bt => new ReadBoxTagDto
                    {
                        id_box = bt.id_box,
                        id_tag = bt.id_tag
                    }).ToArray() : null,
                item_boxs = expand != null && expand.Contains("item_boxs") ? s.ItemsBoxs
                    .Select(ib => new ReadItemBoxDto
                    {
                        id_box = ib.id_box,
                        id_item = ib.id_item,
                        qte_item_box = ib.qte_item_box,
                        seuil_max_item_item_box = ib.seuil_max_item_item_box
                    }).ToArray() : null,
                box_tags_count = s.BoxsTags.Count,
                item_boxs_count = s.ItemsBoxs.Count
            }).FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Box with id {id} not found");
    }

    public async Task<ReadBoxDto> CreateBox(CreateBoxDto boxDto)
    {
        // end position must be greater than start position
        if (boxDto.xend_box <= boxDto.xstart_box || boxDto.yend_box <= boxDto.ystart_box)
        {
            throw new ArgumentException("End position must be greater than start position");
        }
        // check if the store exists
        var store = await _context.Stores.FindAsync(boxDto.id_store) ?? throw new KeyNotFoundException($"Store with id {boxDto.id_store} not found");
        // check if the box XY position is not bigger than the store XY length
        if (boxDto.xend_box > store.xlength_store || boxDto.yend_box > store.ylength_store)
        {
            throw new ArgumentException("Box XY position is bigger than the store XY length");
        }
        // check if a box in the same store has a XY position already taken
        // (((NXS > OXS && NXS < OXE) || (NXE < OXE && NXE > OXS)) && ((NYS > OYS && NYS < OYE) || (NYE < OYE && NYE > OYS)))
        // N = new box, O = old box
        // X = x position, Y = y position, S = start, E = end
        if (await _context.Boxs.AnyAsync(b => b.id_store == boxDto.id_store &&
                                              ((boxDto.xstart_box <= b.xstart_box && boxDto.xend_box > b.xstart_box) ||
                                               (boxDto.xstart_box >= b.xstart_box && boxDto.xstart_box < b.xend_box)) &&
                                              ((boxDto.ystart_box <= b.ystart_box && boxDto.yend_box > b.ystart_box) ||
                                               (boxDto.ystart_box >= b.ystart_box && boxDto.ystart_box < b.yend_box))))
        {
            throw new ArgumentException("Box XY position already taken");
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

    public async Task<ReadBulkBoxDto> CreateBulkBox(List<CreateBoxDto> boxsDto)
    {
        var validQuery = new List<ReadBoxDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var boxDto in boxsDto)
        {
            try
            {
                // end position must be greater than start position
                if (boxDto.xend_box <= boxDto.xstart_box || boxDto.yend_box <= boxDto.ystart_box)
                {
                    throw new ArgumentException("End position must be greater than start position");
                }
                // check if the store exists
                var store = await _context.Stores.FindAsync(boxDto.id_store) ?? throw new KeyNotFoundException($"Store with id {boxDto.id_store} not found");
                // check if the box XY position is not bigger than the store XY length
                if (boxDto.xend_box > store.xlength_store || boxDto.yend_box > store.ylength_store)
                {
                    throw new ArgumentException("Box XY position is bigger than the store XY length");
                }
                // check if a box in the same store has a XY position already taken
                // (((NXS > OXS && NXS < OXE) || (NXE < OXE && NXE > OXS)) && ((NYS > OYS && NYS < OYE) || (NYE < OYE && NYE > OYS)))
                // N = new box, O = old box
                // X = x position, Y = y position, S = start, E = end
                if (await _context.Boxs.AnyAsync(b => b.id_store == boxDto.id_store &&
                                                    ((boxDto.xstart_box <= b.xstart_box && boxDto.xend_box > b.xstart_box) ||
                                                    (boxDto.xstart_box >= b.xstart_box && boxDto.xstart_box < b.xend_box)) &&
                                                    ((boxDto.ystart_box <= b.ystart_box && boxDto.yend_box > b.ystart_box) ||
                                                    (boxDto.ystart_box >= b.ystart_box && boxDto.ystart_box < b.yend_box))))
                {
                    throw new ArgumentException("Box XY position already taken");
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
                validQuery.Add(new ReadBoxDto
                {
                    id_box = newBox.id_box,
                    xstart_box = newBox.xstart_box,
                    ystart_box = newBox.ystart_box,
                    yend_box = newBox.yend_box,
                    xend_box = newBox.xend_box,
                    id_store = newBox.id_store
                });
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = boxDto
                });
            }
        }
        if (errorQuery.Count == 0)
        {
            await _context.SaveChangesAsync();
        }
        return new ReadBulkBoxDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }

    public async Task<ReadBoxDto> UpdateBox(int id, UpdateBoxDto boxDto, int? storeId = null)
    {
        var boxToUpdate = await _context.Boxs.FindAsync(id);
        bool newXYPosition = false;
        if ((boxToUpdate is null) || (storeId is not null && boxToUpdate.id_store != storeId))
        {
            throw new KeyNotFoundException($"Box with id {id} not found");
        }
        if (boxDto.xstart_box is not null)
        {
            boxToUpdate.xstart_box = boxDto.xstart_box.Value;
            newXYPosition = true;
        }
        if (boxDto.ystart_box is not null)
        {
            boxToUpdate.ystart_box = boxDto.ystart_box.Value;
            newXYPosition = true;
        }
        if (boxDto.yend_box is not null)
        {
            boxToUpdate.yend_box = boxDto.yend_box.Value;
            newXYPosition = true;
        }
        if (boxDto.xend_box is not null)
        {
            boxToUpdate.xend_box = boxDto.xend_box.Value;
            newXYPosition = true;
        }
        // end position must be greater than start position
        if (boxToUpdate.xend_box <= boxToUpdate.xstart_box || boxToUpdate.yend_box <= boxToUpdate.ystart_box)
        {
            throw new ArgumentException("End position must be greater than start position");
        }
        if (boxDto.new_id_store is not null)
        {
            if (!await _context.Stores.AnyAsync(s => s.id_store == boxDto.new_id_store))
            {
                throw new KeyNotFoundException($"Store with id {boxDto.new_id_store} not found");
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
                                              ((boxDto.xstart_box <= b.xstart_box && boxDto.xend_box > b.xstart_box) ||
                                               (boxDto.xstart_box >= b.xstart_box && boxDto.xstart_box < b.xend_box)) &&
                                              ((boxDto.ystart_box <= b.ystart_box && boxDto.yend_box > b.ystart_box) ||
                                               (boxDto.ystart_box >= b.ystart_box && boxDto.ystart_box < b.yend_box))))
            {
                throw new ArgumentException("Box XY position already taken");
            }
        }
        // check if the box XY position is not bigger than the store XY length
        var store = await _context.Stores.FindAsync(boxToUpdate.id_store) ?? throw new KeyNotFoundException($"Store with id {boxToUpdate.id_store} not found");
        if (boxToUpdate.xend_box > store.xlength_store || boxToUpdate.yend_box > store.ylength_store)
        {
            throw new ArgumentException("Box XY position is bigger than the store XY length");
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

    public async Task<ReadBulkBoxDto> UpdateBulkBox(List<UpdateBuckBoxByStoreDto> boxsDto, int? storeId = null)
    {
        var validQuery = new List<ReadBoxDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var boxDto in boxsDto)
        {
            try
            {
                var boxToUpdate = await _context.Boxs.FindAsync(boxDto.id_box);
                if ((boxToUpdate is null) || (storeId is not null && boxToUpdate.id_store != storeId))
                {
                    throw new KeyNotFoundException($"Box with id {boxDto.id_box} not found");
                }
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
                // end position must be greater than start position
                if (boxToUpdate.xend_box <= boxToUpdate.xstart_box || boxToUpdate.yend_box <= boxToUpdate.ystart_box)
                {
                    throw new ArgumentException("End position must be greater than start position");
                }
                if (boxDto.new_id_store is not null)
                {
                    if (!await _context.Stores.AnyAsync(s => s.id_store == boxDto.new_id_store))
                    {
                        throw new KeyNotFoundException($"Store with id {boxDto.new_id_store} not found");
                    }
                    boxToUpdate.id_store = boxDto.new_id_store.Value;
                }
                // check if the box XY position is not bigger than the store XY length
                var store = await _context.Stores.FindAsync(boxToUpdate.id_store) ?? throw new KeyNotFoundException($"Store with id {boxToUpdate.id_store} not found");
                if (boxToUpdate.xend_box > store.xlength_store || boxToUpdate.yend_box > store.ylength_store)
                {
                    throw new ArgumentException("Box XY position is bigger than the store XY length");
                }
                validQuery.Add(new ReadBoxDto
                {
                    id_box = boxToUpdate.id_box,
                    xstart_box = boxToUpdate.xstart_box,
                    ystart_box = boxToUpdate.ystart_box,
                    yend_box = boxToUpdate.yend_box,
                    xend_box = boxToUpdate.xend_box,
                    id_store = boxToUpdate.id_store
                });
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = boxDto
                });
            }
        }
        // if there is no error, 1: check if no box as the same XY position in the same store, 2: save changes if still no error
        if (errorQuery.Count == 0)
        {
            foreach (var boxDto in boxsDto)
            {
                try
                {
                    var boxToUpdate = await _context.Boxs.FindAsync(boxDto.id_box) ?? throw new KeyNotFoundException($"Box with id {boxDto.id_box} not found");
                    if (await _context.Boxs.AnyAsync(b => b.id_store == boxToUpdate.id_store && b.id_box != boxToUpdate.id_box &&
                                                        ((boxDto.xstart_box <= b.xstart_box && boxDto.xend_box > b.xstart_box) ||
                                                        (boxDto.xstart_box >= b.xstart_box && boxDto.xstart_box < b.xend_box)) &&
                                                        ((boxDto.ystart_box <= b.ystart_box && boxDto.yend_box > b.ystart_box) ||
                                                        (boxDto.ystart_box >= b.ystart_box && boxDto.ystart_box < b.yend_box))))
                    {
                        throw new ArgumentException("Box XY position already taken");
                    }
                }
                catch (Exception e)
                {
                    errorQuery.Add(new ErrorDetail
                    {
                        Reason = e.Message,
                        Data = boxDto
                    });
                }
            }
            if (errorQuery.Count == 0)
            {
                await _context.SaveChangesAsync();
            }
        }
        return new ReadBulkBoxDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }

    public async Task DeleteBox(int id, int? storeId = null)
    {
        var boxToDelete = await _context.Boxs.FindAsync(id);
        if ((boxToDelete is null) || (storeId is not null && boxToDelete.id_store != storeId))
        {
            throw new KeyNotFoundException($"Box with id {id} not found");
        }
        // check if the box has a item in it (ItemsBoxs) with qte_item_box > 0
        if (await _context.ItemsBoxs.AnyAsync(ib => ib.id_box == id && ib.qte_item_box > 0))
        {
            throw new InvalidOperationException($"Box with id {id} has items in it");
        }
        _context.Boxs.Remove(boxToDelete);
        await _context.SaveChangesAsync();
    }

    public async Task<ReadBulkBoxDto> DeleteBulkBox(List<int> ids, int storeId)
    {
        var validQuery = new List<ReadBoxDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var id in ids)
        {
            try
            {
                await DeleteBox(id, storeId);
                validQuery.Add(new ReadBoxDto
                {
                    id_box = id
                });
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = id
                });
            }
        }
        return new ReadBulkBoxDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }
}