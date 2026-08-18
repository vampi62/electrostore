using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Kafka.Messages;
using ElectrostoreAPI.Kafka.Producer;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.EncryptionService;
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Services.ValidateStoreService;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Text.Json;

namespace ElectrostoreAPI.Services.StoreService;

public class StoreService : IStoreService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly string _encryptionKey;
    private readonly ISessionService _sessionService;
    private readonly IEncryptionService _encryptionService;
    private readonly IValidateStoreService _validateStoreService;
    private readonly IKafkaProducerService _kafkaProducer;

    public StoreService(IMapper mapper, ApplicationDbContext context, IConfiguration configuration, IEncryptionService encryptionService, ISessionService sessionService, IValidateStoreService validateStoreService, IKafkaProducerService kafkaProducer)
    {
        _mapper = mapper;
        _context = context;
        _encryptionKey = configuration.GetValue<string>("Encryption:HexKey") ?? throw new InvalidOperationException("Encryption key is not configured");
        _sessionService = sessionService;
        _encryptionService = encryptionService;
        _validateStoreService = validateStoreService;
        _kafkaProducer = kafkaProducer;
    }

    // limit the number of store to 100 and add offset and search parameters
    public async Task<PaginatedResponseDto<ReadExtendedStoreDto>> GetStores(int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null, List<int>? idResearch = null)
    {
        var query = _context.Stores.AsQueryable();
        var filterResult = default(Expression<Func<Stores, bool>>);
        if (idResearch is not null && idResearch.Count > 0)
        {
            query = query.Where(s => idResearch.Contains(s.id_store));
        }
        else
        {
            if (rsql != null && rsql.Count > 0)
            {
                (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<Stores>(rsql);
                query = query.Where(filterResult);
            }
            if (!string.IsNullOrEmpty(sort?.Field))
            {
                var sortResult = RsqlParserExtensions.ToSortExpression<Stores>(sort);
                if (sortResult.Item1 != null)
                {
                    query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
                }
                else
                {
                    sort = new SorterDto { Field = "id_store", Order = "asc" };
                    query = query.OrderBy(s => s.id_store);
                }
            }
            else
            {
                query = query.OrderBy(s => s.id_store);
            }
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
                StoresTags = expand != null && expand.Contains("stores_tags") ? s.StoresTags.Take(20).ToList() : null,
                Zone = expand != null && expand.Contains("zone") ? s.Zone : null
            })
            .ToListAsync();
        return new PaginatedResponseDto<ReadExtendedStoreDto>
        {
            data = store.Select(s =>
            {
                return _mapper.Map<ReadExtendedStoreDto>(s.Store) with
                {
                    mqtt_password_store = string.Empty, // Do not return the password in the list view
                    boxs_count = s.BoxsCount,
                    leds_count = s.LedsCount,
                    stores_tags_count = s.StoresTagsCount,
                    boxs = _mapper.Map<IEnumerable<ReadBoxDto>>(s.Boxs),
                    leds = _mapper.Map<IEnumerable<ReadLedDto>>(s.Leds),
                    stores_tags = _mapper.Map<IEnumerable<ReadStoreTagDto>>(s.StoresTags),
                    zone = _mapper.Map<ReadZoneDto?>(s.Zone)
                };
            }).ToList(),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.Stores.CountAsync(filterResult ?? (s => true)),
                nextOffset = offset + limit,
                hasMore = await _context.Stores.Skip(offset + limit).AnyAsync(filterResult ?? (s => true))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
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
                StoresTags = expand != null && expand.Contains("stores_tags") ? s.StoresTags.Take(20).ToList() : null,
                Zone = expand != null && expand.Contains("zone") ? s.Zone : null
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Store with id '{id}' not found");
        var clientRole = _sessionService.GetClientRole();
        var mqttPassword = string.Empty;
        if (clientRole == UserRole.Admin)
        {
            mqttPassword = await _encryptionService.Decrypt(new EncryptDto
            {
                EncryptedData = store.Store.mqtt_password_store,
                IV = store.Store.mqtt_password_encryption_iv_store,
                Tag = store.Store.mqtt_password_encryption_tag_store
            }, _encryptionKey);
        }
        return _mapper.Map<ReadExtendedStoreDto>(store.Store) with
        {
            mqtt_password_store = mqttPassword,
            boxs_count = store.BoxsCount,
            leds_count = store.LedsCount,
            stores_tags_count = store.StoresTagsCount,
            boxs = _mapper.Map<IEnumerable<ReadBoxDto>>(store.Boxs),
            leds = _mapper.Map<IEnumerable<ReadLedDto>>(store.Leds),
            stores_tags = _mapper.Map<IEnumerable<ReadStoreTagDto>>(store.StoresTags),
            zone = _mapper.Map<ReadZoneDto?>(store.Zone)
        };
    }

    public async Task<ReadStoreDto> CreateStore(CreateStoreDto storeDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You do not have permission to create a store");
        }
        if (storeDto.id_zone is not null && !await _context.Zones.AnyAsync(z => z.id_zone == storeDto.id_zone))
        {
            throw new KeyNotFoundException($"Zone with id '{storeDto.id_zone}' not found");
        }
        var newStore = _mapper.Map<Stores>(storeDto);
        _validateStoreService.ValidateStorePlanPosition(newStore);
        var mqttPassword = GenerateMqttPasswordForStore();
        var encryptedPassword = await _encryptionService.Encrypt(mqttPassword, _encryptionKey);
        newStore.mqtt_password_store = encryptedPassword.EncryptedData;
        newStore.mqtt_password_encryption_iv_store = encryptedPassword.IV;
        newStore.mqtt_password_encryption_tag_store = encryptedPassword.Tag;
        _context.Stores.Add(newStore);
        await _context.SaveChangesAsync();
        await _kafkaProducer.PublishAsync(
            "mqtt-user-events",
            newStore.id_store.ToString(),
            JsonSerializer.Serialize(new MqttUserMessage
            {
                user = newStore.mqtt_name_store,
                password = mqttPassword,
                delete = false
            })
        );
        return _mapper.Map<ReadStoreDto>(newStore) with
        {
            mqtt_password_store = mqttPassword
        };
    }

    public async Task<ReadStoreDto> UpdateStore(int id, UpdateStoreDto storeDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You do not have permission to update a store");
        }
        var storeToUpdate = await _context.Stores.FindAsync(id) ?? throw new KeyNotFoundException($"Store with id '{id}' not found");
        var oldMqttName = storeToUpdate.mqtt_name_store;
        await _validateStoreService.UpdateStoreInformations(storeToUpdate, storeDto);
        _validateStoreService.ValidateStorePlanPosition(storeToUpdate);
        await _validateStoreService.CheckUpdateStoreOutsideElement(storeToUpdate);
        var mqttPassword = string.Empty;
        if (storeDto.reset_mqtt_password_store == true)
        {
            mqttPassword = GenerateMqttPasswordForStore();
            var encryptedPassword = await _encryptionService.Encrypt(mqttPassword, _encryptionKey);
            storeToUpdate.mqtt_password_store = encryptedPassword.EncryptedData;
            storeToUpdate.mqtt_password_encryption_iv_store = encryptedPassword.IV;
            storeToUpdate.mqtt_password_encryption_tag_store = encryptedPassword.Tag;
            await _kafkaProducer.PublishAsync(
                "mqtt-user-events",
                storeToUpdate.id_store.ToString(),
                JsonSerializer.Serialize(new MqttUserMessage
                {
                    user = storeToUpdate.mqtt_name_store,
                    old_user = oldMqttName,
                    password = mqttPassword,
                    delete = false
                })
            );
        }
        await _context.SaveChangesAsync();
        if (mqttPassword == string.Empty)
        {
            mqttPassword = await _encryptionService.Decrypt(new EncryptDto
            {
                EncryptedData = storeToUpdate.mqtt_password_store,
                IV = storeToUpdate.mqtt_password_encryption_iv_store,
                Tag = storeToUpdate.mqtt_password_encryption_tag_store
            }, _encryptionKey);
        }
        return _mapper.Map<ReadStoreDto>(storeToUpdate) with
        {
            mqtt_password_store = mqttPassword
        };
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
        await _kafkaProducer.PublishAsync(
            "mqtt-user-events",
            storeToDelete.id_store.ToString(),
            JsonSerializer.Serialize(new MqttUserMessage
            {
                user = storeToDelete.mqtt_name_store,
                password = "",
                delete = true
            })
        );
        await _context.SaveChangesAsync();
    }

    public async Task<ReadStoreCompleteDto> CreateStoreComplete(CreateStoreCompleteDto storeDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You do not have permission to create a store");
        }
        if (storeDto.store.id_zone is not null && !await _context.Zones.AnyAsync(z => z.id_zone == storeDto.store.id_zone))
        {
            throw new KeyNotFoundException($"Zone with id '{storeDto.store.id_zone}' not found");
        }
        var newStore = _mapper.Map<Stores>(storeDto.store);
        _validateStoreService.ValidateStorePlanPosition(newStore);
        await using var transaction = await _context.Database.BeginTransactionAsync();
        _context.Stores.Add(newStore);
        await _context.SaveChangesAsync(); // persist store to get real id_store
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
                    mqtt_id_led = ledDto.mqtt_id_led
                };
                var newLed = _mapper.Map<Leds>(ledDtoFull);
                _validateStoreService.ValidateLedPosition(newLed, newStore);
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
        var mqttPassword = GenerateMqttPasswordForStore();
        var encryptedPassword = await _encryptionService.Encrypt(mqttPassword, _encryptionKey);
        newStore.mqtt_password_store = encryptedPassword.EncryptedData;
        newStore.mqtt_password_encryption_iv_store = encryptedPassword.IV;
        newStore.mqtt_password_encryption_tag_store = encryptedPassword.Tag;
        await _context.SaveChangesAsync();
        if (errorQueryLed.Count == 0 && errorQueryBox.Count == 0)
        {
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            await _kafkaProducer.PublishAsync(
                "mqtt-user-events",
                newStore.id_store.ToString(),
                JsonSerializer.Serialize(new MqttUserMessage
                {
                    user = newStore.mqtt_name_store,
                    password = mqttPassword,
                    delete = false
                })
            );
        }
        else
        {
            await transaction.RollbackAsync();
        }
        return new ReadStoreCompleteDto
        {
            store = _mapper.Map<ReadStoreDto>(newStore) with
            {
                mqtt_password_store = mqttPassword
            },
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
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var oldMqttName = storeToUpdate.mqtt_name_store;
        await _validateStoreService.UpdateStoreInformations(storeToUpdate, storeDto.store);
        _validateStoreService.ValidateStorePlanPosition(storeToUpdate);
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
        var mqttPassword = string.Empty;
        if (errorQueryLed.Count == 0 && errorQueryBox.Count == 0)
        {
            await _context.SaveChangesAsync();
            if (storeDto.store.reset_mqtt_password_store == true)
            {
                mqttPassword = GenerateMqttPasswordForStore();
                var encryptedPassword = await _encryptionService.Encrypt(mqttPassword, _encryptionKey);
                storeToUpdate.mqtt_password_store = encryptedPassword.EncryptedData;
                storeToUpdate.mqtt_password_encryption_iv_store = encryptedPassword.IV;
                storeToUpdate.mqtt_password_encryption_tag_store = encryptedPassword.Tag;
                await _context.SaveChangesAsync();
                await _kafkaProducer.PublishAsync(
                    "mqtt-user-events",
                    storeToUpdate.id_store.ToString(),
                    JsonSerializer.Serialize(new MqttUserMessage
                    {
                        user = storeToUpdate.mqtt_name_store,
                        old_user = oldMqttName,
                        password = mqttPassword,
                        delete = false
                    })
                );
            }
            await transaction.CommitAsync();
        }
        else
        {
            await transaction.RollbackAsync();
        }
        if (mqttPassword == string.Empty)
        {
            mqttPassword = await _encryptionService.Decrypt(new EncryptDto
            {
                EncryptedData = storeToUpdate.mqtt_password_store,
                IV = storeToUpdate.mqtt_password_encryption_iv_store,
                Tag = storeToUpdate.mqtt_password_encryption_tag_store
            }, _encryptionKey);
        }
        return new ReadStoreCompleteDto
        {
            store = _mapper.Map<ReadStoreDto>(storeToUpdate) with
            {
                mqtt_password_store = mqttPassword
            },
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

    public async Task<int> UpdateStoreMqttStatusByMqttNameAsync(string mqttName, UpdateStoreMqttStatusDto mqttStatusDto, CancellationToken cancellationToken)
    {
        var stores = await _context.Stores
            .Where(s => s.mqtt_name_store == mqttName)
            .ToListAsync(cancellationToken);
        if (stores.Count == 0)
        {
            return 0;
        }
        var now = DateTime.UtcNow;
        foreach (var store in stores)
        {
            store.is_mqtt_connected_store = mqttStatusDto.is_mqtt_connected_store;
            store.mqtt_last_seen_store    = now;
        }
        await _context.SaveChangesAsync(cancellationToken);
        return stores.Count;
    }

    private static string GenerateMqttPasswordForStore()
    {
        var randomBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(24);
        return Convert.ToBase64String(randomBytes);
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
                        mqtt_id_led = led.mqtt_id_led ?? throw new ArgumentException("mqtt_id_led is required for new led")
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