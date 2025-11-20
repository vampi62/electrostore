using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using electrostore.Enums;
using electrostore.Services.SessionService;
using electrostore.Services.ValidateStoreService;

namespace electrostore.Services.StoreService;

public class StoreService : IStoreService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;
    private readonly IValidateStoreService _validateStoreService;

    public StoreService(IMapper mapper, ApplicationDbContext context, ISessionService sessionService, IValidateStoreService validateStoreService)
    {
        _mapper = mapper;
        _context = context;
        _sessionService = sessionService;
        _validateStoreService = validateStoreService;
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
        await _validateStoreService.UpdateStoreInformations(storeToUpdate, storeDto);
        await _validateStoreService.CheckUpdateStoreOutsideElement(storeToUpdate);
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
                var ledDtoFull = new CreateLedDto
                {
                    x_led = ledDto.x_led,
                    y_led = ledDto.y_led,
                    id_store = newStore.id_store,
                    mqtt_led_id = ledDto.mqtt_led_id
                };
                var newLed = _mapper.Map<Leds>(ledDtoFull);
                _validateStoreService.ValidateLedPosition(newLed, newStore);
                _context.Leds.Add(newLed);
                await _context.SaveChangesAsync();
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
                var boxDtoFull = new CreateBoxDto
                {
                    xstart_box = boxDto.xstart_box,
                    ystart_box = boxDto.ystart_box,
                    xend_box = boxDto.xend_box,
                    yend_box = boxDto.yend_box,
                    id_store = newStore.id_store
                };
                await _validateStoreService.CheckCreateBoxPositionOverlap(boxDtoFull);
                var newBox = _mapper.Map<Boxs>(boxDtoFull);
                _validateStoreService.ValidateBoxPosition(newBox, newStore);
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
        await _validateStoreService.UpdateStoreInformations(storeToUpdate, storeDto.store);
        // Add leds and boxs, if status field indicate the new status "delete", "modified", "new"
        (var validQueryLed, var errorQueryLed) = await UpdateLedList(storeToUpdate, storeDto.leds ?? []);
        (var validQueryBox, var errorQueryBox) = await UpdateBoxList(storeToUpdate, storeDto.boxs ?? []);
        await _validateStoreService.CheckUpdateStoreOutsideElement(storeToUpdate);
        if (errorQueryBox.Count == 0)
        {
            // Check for overlapping boxs after all modifications
            foreach (var box in storeDto.boxs ?? [])
            {
                try
                {
                    if (box.status == "new" || box.status == "modified")
                    {
                        var boxToUpdate = await _context.Boxs.FindAsync(box.id_box) ?? throw new KeyNotFoundException($"Box with id '{box.id_box}' not found");
                        await _validateStoreService.CheckUpdateBoxPositionOverlap(boxToUpdate);
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

    private async Task<(List<ReadLedDto>, List<ErrorDetail>)> UpdateLedList(Stores storeToUpdate, IEnumerable<UpdateBulkLedByStoreDto> ledListDto)
    {
        var validQueryLed = new List<ReadLedDto>();
        var errorQueryLed = new List<ErrorDetail>();
        foreach (var led in ledListDto)
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
                    await _validateStoreService.UpdateLedInformations(ledToUpdate, _mapper.Map<UpdateLedDto>(led));
                    _validateStoreService.ValidateLedPosition(ledToUpdate, storeToUpdate);
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
                    _validateStoreService.ValidateLedPosition(newLed, storeToUpdate);
                    _context.Leds.Add(newLed);
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
        return (validQueryLed, errorQueryLed);
    }
    
    private async Task<(List<ReadBoxDto>, List<ErrorDetail>)> UpdateBoxList(Stores storeToUpdate, IEnumerable<UpdateBulkBoxByStoreDto> boxListDto)
    {
        var validQueryBox = new List<ReadBoxDto>();
        var errorQueryBox = new List<ErrorDetail>();
        foreach (var box in boxListDto)
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
                    await _validateStoreService.UpdateBoxInformations(boxToUpdate, _mapper.Map<UpdateBoxDto>(box));
                    _validateStoreService.ValidateBoxPosition(boxToUpdate, storeToUpdate);
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
                    _validateStoreService.ValidateBoxPosition(newBox, storeToUpdate);
                    _context.Boxs.Add(newBox);
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
        return (validQueryBox, errorQueryBox);
    }
}