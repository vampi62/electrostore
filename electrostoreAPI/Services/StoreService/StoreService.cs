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
        return store.Select(s =>
        {
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
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Store with id '{id}' not found");
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
        var newStore = _mapper.Map<Stores>(storeDto);
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
        var storeToUpdate = await _context.Stores.FindAsync(id) ?? throw new KeyNotFoundException($"Store with id '{id}' not found");
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
        var storeToDelete = await _context.Stores.FindAsync(id) ?? throw new KeyNotFoundException($"Store with id '{id}' not found");
        _context.Stores.Remove(storeToDelete);
        await _context.SaveChangesAsync();
    }

    public async Task<ReadStoreCompleteDto> CreateStoreComplete(CreateStoreCompleteDto storeDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You do not have permission to create a store");
        }
        var newStore = _mapper.Map<Stores>(storeDto.store);
        _context.Stores.Add(newStore);
        await _context.SaveChangesAsync();

        // Add leds and boxs if provided
        var validQueryLed = new List<ReadLedDto>();
        var errorQueryLed = new List<ErrorDetail>();
        foreach (var ledDto in storeDto.leds ?? Enumerable.Empty<CreateLedByStoreDto>())
        {
            try
            {
                if (ledDto.x_led < 0 || ledDto.x_led >= newStore.xlength_store ||
                    ledDto.y_led < 0 || ledDto.y_led >= newStore.ylength_store)
                {
                    throw new ArgumentException($"Led position ({ledDto.x_led}, {ledDto.y_led}) is out of store bounds.");
                }
                var ledDtoFull = new CreateLedDto
                {
                    x_led = ledDto.x_led,
                    y_led = ledDto.y_led,
                    id_store = newStore.id_store,
                    mqtt_led_id = ledDto.mqtt_led_id
                };
                var newLed = _mapper.Map<Leds>(ledDtoFull);
                _context.Leds.Add(newLed);
                validQueryLed.Add(_mapper.Map<ReadLedDto>(newLed));
            }
            catch (Exception e)
            {
                errorQueryLed.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = ledDto
                });
            }
        }
        var validQueryBox = new List<ReadBoxDto>();
        var errorQueryBox = new List<ErrorDetail>();
        foreach (var boxDto in storeDto.boxs ?? Enumerable.Empty<CreateBoxByStoreDto>())
        {
            try
            {
                // end position must be greater than start position
                if (boxDto.xstart_box >= boxDto.xend_box || boxDto.ystart_box >= boxDto.yend_box)
                {
                    throw new ArgumentException($"Box start position ({boxDto.xstart_box}, {boxDto.ystart_box}) must be less than end position ({boxDto.xend_box}, {boxDto.yend_box}).");
                }
                if (boxDto.xstart_box < 0 || boxDto.xend_box > newStore.xlength_store ||
                    boxDto.ystart_box < 0 || boxDto.yend_box > newStore.ylength_store)
                {
                    throw new ArgumentException($"Box position ({boxDto.xstart_box}, {boxDto.ystart_box}, {boxDto.xend_box}, {boxDto.yend_box}) is out of store bounds.");
                }
                // Check for overlapping boxs
                if (await _context.Boxs.AnyAsync(b =>
                    b.id_store == newStore.id_store &&
                    b.xstart_box < boxDto.xend_box && b.xend_box > boxDto.xstart_box &&
                    b.ystart_box < boxDto.yend_box && b.yend_box > boxDto.ystart_box))
                {
                    throw new ArgumentException("Box overlaps with an existing box in the store.");
                }
                var boxDtoFull = new CreateBoxDto
                {
                    xstart_box = boxDto.xstart_box,
                    ystart_box = boxDto.ystart_box,
                    xend_box = boxDto.xend_box,
                    yend_box = boxDto.yend_box,
                    id_store = newStore.id_store
                };
                var newBox = _mapper.Map<Boxs>(boxDtoFull);
                _context.Boxs.Add(newBox);
                validQueryBox.Add(_mapper.Map<ReadBoxDto>(newBox));
            }
            catch (Exception e)
            {
                errorQueryBox.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = boxDto
                });
            }
        }
        if (errorQueryLed.Count == 0 && errorQueryBox.Count == 0)
        {
            await _context.SaveChangesAsync();
        }
        return new ReadStoreCompleteDto
        {
            store = _mapper.Map<ReadStoreDto>(newStore),
            leds = new ReadBulkLedDto
            {
                Valide = validQueryLed,
                Error = errorQueryLed
            },
            boxs = new ReadBulkBoxDto
            {
                Valide = validQueryBox,
                Error = errorQueryBox
            }
        };
    }
    public async Task<ReadStoreCompleteDto> UpdateStoreComplete(int id, UpdateStoreCompleteDto storeDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You do not have permission to update a store");
        }
        var storeToUpdate = await _context.Stores.FindAsync(id) ?? throw new KeyNotFoundException($"Store with id '{id}' not found");
        if (storeDto.store.nom_store is not null)
        {
            storeToUpdate.nom_store = storeDto.store.nom_store;
        }
        if (storeDto.store.xlength_store is not null)
        {
            storeToUpdate.xlength_store = storeDto.store.xlength_store.Value;
        }
        if (storeDto.store.ylength_store is not null)
        {
            storeToUpdate.ylength_store = storeDto.store.ylength_store.Value;
        }
        if (storeDto.store.mqtt_name_store is not null)
        {
            storeToUpdate.mqtt_name_store = storeDto.store.mqtt_name_store;
        }
        // Add leds and boxs, if status field indicate the new status "delete", "modified", "new"
        var validQueryLed = new List<ReadLedDto>();
        var errorQueryLed = new List<ErrorDetail>();
        foreach (var led in storeDto.leds ?? Enumerable.Empty<UpdateBulkLedByStoreDto>())
        {
            try
            {
                if (led.status == "delete")
                {
                    var ledToDelete = await _context.Leds.FindAsync(led.id_led) ?? throw new KeyNotFoundException($"Led with id '{led.id_led}' not found");
                    if (ledToDelete.id_store != storeToUpdate.id_store)
                    {
                        throw new ArgumentException($"Led with id '{led.id_led}' does not belong to the store with id '{storeToUpdate.id_store}'.");
                    }
                    _context.Leds.Remove(ledToDelete);
                    continue;
                }
                else if (led.status == "modified")
                {
                    var ledToUpdate = await _context.Leds.FindAsync(led.id_led) ?? throw new KeyNotFoundException($"Led with id '{led.id_led}' not found");
                    if (ledToUpdate.id_store != storeToUpdate.id_store)
                    {
                        throw new ArgumentException($"Led with id '{led.id_led}' does not belong to the store with id '{storeToUpdate.id_store}'.");
                    }
                    if (led.x_led is not null)
                    {
                        ledToUpdate.x_led = led.x_led.Value;
                    }
                    if (led.y_led is not null)
                    {
                        ledToUpdate.y_led = led.y_led.Value;
                    }
                    if (led.mqtt_led_id is not null)
                    {
                        ledToUpdate.mqtt_led_id = led.mqtt_led_id.Value;
                    }
                    // check if led position is within store bounds
                    if (ledToUpdate.x_led < 0 || ledToUpdate.x_led >= storeToUpdate.xlength_store ||
                        ledToUpdate.y_led < 0 || ledToUpdate.y_led >= storeToUpdate.ylength_store)
                    {
                        throw new ArgumentException($"Led position ({ledToUpdate.x_led}, {ledToUpdate.y_led}) is out of store bounds.");
                    }
                    validQueryLed.Add(_mapper.Map<ReadLedDto>(ledToUpdate));
                }
                else if (led.status == "new")
                {
                    var ledDtoFull = new CreateLedDto
                    {
                        x_led = led.x_led ?? throw new ArgumentException("x_led is required for new led"),
                        y_led = led.y_led ?? throw new ArgumentException("y_led is required for new led"),
                        id_store = storeToUpdate.id_store,
                        mqtt_led_id = led.mqtt_led_id ?? throw new ArgumentException("mqtt_led_id is required for new led")
                    };
                    var newLed = _mapper.Map<Leds>(ledDtoFull);
                    _context.Leds.Add(newLed);
                    // check if led position is within store bounds
                    if (newLed.x_led < 0 || newLed.x_led >= storeToUpdate.xlength_store ||
                        newLed.y_led < 0 || newLed.y_led >= storeToUpdate.ylength_store)
                    {
                        throw new ArgumentException($"Led position ({newLed.x_led}, {newLed.y_led}) is out of store bounds.");
                    }
                    validQueryLed.Add(_mapper.Map<ReadLedDto>(newLed));
                }
            }
            catch (Exception e)
            {
                errorQueryLed.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = led
                });
            }
        }
        var validQueryBox = new List<ReadBoxDto>();
        var errorQueryBox = new List<ErrorDetail>();
        foreach (var box in storeDto.boxs ?? Enumerable.Empty<UpdateBulkBoxByStoreDto>())
        {
            try
            {
                if (box.status == "delete")
                {
                    var boxToDelete = await _context.Boxs.FindAsync(box.id_box) ?? throw new KeyNotFoundException($"Box with id '{box.id_box}' not found");
                    if (boxToDelete.id_store != storeToUpdate.id_store)
                    {
                        throw new ArgumentException($"Box with id '{box.id_box}' does not belong to the store with id '{storeToUpdate.id_store}'.");
                    }
                    _context.Boxs.Remove(boxToDelete);
                    continue;
                }
                else if (box.status == "modified")
                {
                    var boxToUpdate = await _context.Boxs.FindAsync(box.id_box) ?? throw new KeyNotFoundException($"Box with id '{box.id_box}' not found");
                    if (boxToUpdate.id_store != storeToUpdate.id_store)
                    {
                        throw new ArgumentException($"Box with id '{box.id_box}' does not belong to the store with id '{storeToUpdate.id_store}'.");
                    }
                    if (box.xstart_box is not null)
                    {
                        boxToUpdate.xstart_box = box.xstart_box.Value;
                    }
                    if (box.xend_box is not null)
                    {
                        boxToUpdate.xend_box = box.xend_box.Value;
                    }
                    if (box.ystart_box is not null)
                    {
                        boxToUpdate.ystart_box = box.ystart_box.Value;
                    }
                    if (box.yend_box is not null)
                    {
                        boxToUpdate.yend_box = box.yend_box.Value;
                    }
                    // end position must be greater than start position
                    if (boxToUpdate.xstart_box >= boxToUpdate.xend_box || boxToUpdate.ystart_box >= boxToUpdate.yend_box)
                    {
                        throw new ArgumentException($"Box start position ({boxToUpdate.xstart_box}, {boxToUpdate.ystart_box}) must be less than end position ({boxToUpdate.xend_box}, {boxToUpdate.yend_box}).");
                    }
                    // check if box position is within store bounds
                    if (boxToUpdate.xstart_box < 0 || boxToUpdate.xend_box > storeToUpdate.xlength_store ||
                        boxToUpdate.ystart_box < 0 || boxToUpdate.yend_box > storeToUpdate.ylength_store)
                    {
                        throw new ArgumentException($"Box position ({boxToUpdate.xstart_box}, {boxToUpdate.ystart_box}, {boxToUpdate.xend_box}, {boxToUpdate.yend_box}) is out of store bounds.");
                    }
                    validQueryBox.Add(_mapper.Map<ReadBoxDto>(boxToUpdate));
                }
                else if (box.status == "new")
                {
                    var boxDtoFull = new CreateBoxDto
                    {
                        xstart_box = box.xstart_box ?? throw new ArgumentException("xstart_box is required for new box"),
                        ystart_box = box.ystart_box ?? throw new ArgumentException("ystart_box is required for new box"),
                        xend_box = box.xend_box ?? throw new ArgumentException("xend_box is required for new box"),
                        yend_box = box.yend_box ?? throw new ArgumentException("yend_box is required for new box"),
                        id_store = storeToUpdate.id_store
                    };
                    var newBox = _mapper.Map<Boxs>(boxDtoFull);
                    _context.Boxs.Add(newBox);
                    // end position must be greater than start position
                    if (newBox.xstart_box >= newBox.xend_box || newBox.ystart_box >= newBox.yend_box)
                    {
                        throw new ArgumentException($"Box start position ({newBox.xstart_box}, {newBox.ystart_box}) must be less than end position ({newBox.xend_box}, {newBox.yend_box}).");
                    }
                    // check if box position is within store bounds
                    if (newBox.xstart_box < 0 || newBox.xend_box > storeToUpdate.xlength_store ||
                        newBox.ystart_box < 0 || newBox.yend_box > storeToUpdate.ylength_store)
                    {
                        throw new ArgumentException($"Box position ({newBox.xstart_box}, {newBox.ystart_box}, {newBox.xend_box}, {newBox.yend_box}) is out of store bounds.");
                    }
                    validQueryBox.Add(_mapper.Map<ReadBoxDto>(newBox));
                }
            }
            catch (Exception e)
            {
                errorQueryBox.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = box
                });
            }
        }
        if (errorQueryBox.Count == 0)
        {
            // Check for overlapping boxs after all modifications
            foreach (var box in storeDto.boxs ?? Enumerable.Empty<UpdateBulkBoxByStoreDto>())
            {
                try
                {
                    if (box.status == "new" || box.status == "modified")
                    {
                        if (await _context.Boxs.AnyAsync(b =>
                            b.id_store == storeToUpdate.id_store && b.id_box != box.id_box &&
                            b.xstart_box < box.xend_box && b.xend_box > box.xstart_box &&
                            b.ystart_box < box.yend_box && b.yend_box > box.ystart_box))
                        {
                            throw new ArgumentException("Box overlaps with an existing box in the store.");
                        }
                    }
                }
                catch (Exception e)
                {
                    errorQueryBox.Add(new ErrorDetail
                    {
                        Reason = e.Message,
                        Data = box
                    });
                }
            }
        }
        if (errorQueryLed.Count == 0 && errorQueryBox.Count == 0)
        {
            await _context.SaveChangesAsync();
        }
        return new ReadStoreCompleteDto
        {
            store = _mapper.Map<ReadStoreDto>(storeToUpdate),
            leds = new ReadBulkLedDto
            {
                Valide = validQueryLed,
                Error = errorQueryLed
            },
            boxs = new ReadBulkBoxDto
            {
                Valide = validQueryBox,
                Error = errorQueryBox
            }
        };
    }
}