using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Services.ValidateStoreService;
using Microsoft.EntityFrameworkCore;
using MQTTnet;
using MQTTnet.Protocol;
using System.Linq.Expressions;
using System.Text.Json;

namespace ElectrostoreAPI.Services.LedService;

public class LedService : ILedService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IMqttClient _mqttClient;
    private readonly ISessionService _sessionService;
    private readonly IValidateStoreService _validateStoreService;
    private readonly ILogger<LedService> _logger;
    private readonly static int numberLedSentPerMessage = 10;

    public LedService(IMapper mapper, ApplicationDbContext context, IMqttClient mqttClient, ISessionService sessionService, IValidateStoreService validateStoreService, ILogger<LedService> logger)
    {
        _mapper = mapper;
        _context = context;
        _mqttClient = mqttClient;
        _sessionService = sessionService;
        _validateStoreService = validateStoreService;
        _logger = logger;
    }

    public async Task<PaginatedResponseDto<ReadLedDto>> GetLedsByStoreId(int storeId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null)
    {
        _logger.LogDebug("GetLedsByStoreId: storeId={StoreId}, limit={Limit}, offset={Offset}", storeId, limit, offset);
        // check if the store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == storeId))
        {
            _logger.LogWarning("GetLedsByStoreId: store {StoreId} not found", storeId);
            throw new KeyNotFoundException($"Store with id '{storeId}' not found");
        }
        var query = _context.Leds.AsQueryable();
        var filterResult = default(Expression<Func<Leds, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_store", SearchType = "eq", Value = storeId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<Leds>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<Leds>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "id_led", Order = "asc" };
                query = query.OrderBy(l => l.id_led);
            }
        }
        else
        {
            query = query.OrderBy(l => l.id_led);
        }
        query = query.Skip(offset).Take(limit);
        var led = await query.ToListAsync();
        return new PaginatedResponseDto<ReadLedDto>
        {
            data = _mapper.Map<List<ReadLedDto>>(led),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.Leds.CountAsync(filterResult ?? (l => l.id_store == storeId)),
                nextOffset = offset + limit,
                hasMore = await _context.Leds.Skip(offset + limit).AnyAsync(filterResult ?? (l => l.id_store == storeId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadLedDto> GetLedById(int id, int? storeId = null)
    {
        var led = await _context.Leds.FindAsync(id);
        if (led is null)
        {
            _logger.LogWarning("GetLedById: led {LedId} not found", id);
            throw new KeyNotFoundException($"Led with id '{id}' not found");
        }
        if ((storeId is not null) && (led.id_store != storeId))
        {
            _logger.LogWarning("GetLedById: led {LedId} not found in store {StoreId}", id, storeId);
            throw new KeyNotFoundException($"Led with id '{id}' not found in store with id '{storeId}'");
        }
        return _mapper.Map<ReadLedDto>(led);
    }

    public async Task<ReadLedDto> CreateLed(CreateLedDto ledDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            _logger.LogWarning("CreateLed: client role {ClientRole} is not authorized to create a led", clientRole);
            throw new UnauthorizedAccessException("You do not have permission to create a led");
        }
        // check if store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == ledDto.id_store))
        {
            _logger.LogWarning("CreateLed: store {StoreId} not found", ledDto.id_store);
            throw new KeyNotFoundException($"Store with id '{ledDto.id_store}' not found");
        }
        var newLed = _mapper.Map<Leds>(ledDto);
        var store = await _context.Stores.FindAsync(newLed.id_store);
        if (store is null)
        {
            _logger.LogWarning("CreateLed: store {StoreId} not found", newLed.id_store);
            throw new KeyNotFoundException($"Store with id '{newLed.id_store}' not found");
        }
        _validateStoreService.ValidateLedPosition(newLed, store);
        _context.Leds.Add(newLed);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Led {LedId} created in store {StoreId}", newLed.id_led, newLed.id_store);
        return _mapper.Map<ReadLedDto>(newLed);
    }

    public async Task<ReadBulkLedDto> CreateBulkLed(List<CreateLedDto> ledsDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            _logger.LogWarning("CreateBulkLed: client role {ClientRole} is not authorized to create leds", clientRole);
            throw new UnauthorizedAccessException("You do not have permission to create leds");
        }
        var validQuery = new List<ReadLedDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var ledDto in ledsDto)
        {
            try
            {
                validQuery.Add(await CreateLed(ledDto));
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = ledDto
                });
            }
        }
        _logger.LogInformation("CreateBulkLed: {SuccessCount} led(s) created, {ErrorCount} failed", validQuery.Count, errorQuery.Count);
        return new ReadBulkLedDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }

    public async Task<ReadLedDto> UpdateLed(int id, UpdateLedDto ledDto, int? storeId = null)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            _logger.LogWarning("UpdateLed: client role {ClientRole} is not authorized to update a led", clientRole);
            throw new UnauthorizedAccessException("You do not have permission to update a led");
        }
        var ledToUpdate = await _context.Leds.FindAsync(id);
        if (ledToUpdate is null)
        {
            _logger.LogWarning("UpdateLed: led {LedId} not found", id);
            throw new KeyNotFoundException($"Led with id '{id}' not found");
        }
        if ((storeId is not null) && (ledToUpdate.id_store != storeId))
        {
            _logger.LogWarning("UpdateLed: led {LedId} not found in store {StoreId}", id, storeId);
            throw new KeyNotFoundException($"Led with id '{id}' not found in store with id '{storeId}'");
        }
        await _validateStoreService.UpdateLedInformations(ledToUpdate, ledDto);
        var store = await _context.Stores.FindAsync(ledToUpdate.id_store);
        if (store is null)
        {
            _logger.LogWarning("UpdateLed: store {StoreId} not found", ledToUpdate.id_store);
            throw new KeyNotFoundException($"Store with id '{ledToUpdate.id_store}' not found");
        }
        _validateStoreService.ValidateLedPosition(ledToUpdate, store);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Led {LedId} updated", ledToUpdate.id_led);
        return _mapper.Map<ReadLedDto>(ledToUpdate);
    }

    public async Task<ReadBulkLedDto> UpdateBulkLed(List<UpdateBulkLedByStoreDto> ledsDto, int storeId)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            _logger.LogWarning("UpdateBulkLed: client role {ClientRole} is not authorized to update leds", clientRole);
            throw new UnauthorizedAccessException("You do not have permission to update leds");
        }
        var validQuery = new List<ReadLedDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var ledDto in ledsDto)
        {
            try
            {
                var ledDtoFull = new UpdateLedDto
                {
                    x_led = ledDto.x_led,
                    y_led = ledDto.y_led,
                    mqtt_led_id = ledDto.mqtt_led_id
                };
                validQuery.Add(await UpdateLed(ledDto.id_led, ledDtoFull, storeId));
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = ledDto
                });
            }
        }
        _logger.LogInformation("UpdateBulkLed: {SuccessCount} led(s) updated, {ErrorCount} failed for store {StoreId}", validQuery.Count, errorQuery.Count, storeId);
        return new ReadBulkLedDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }

    public async Task DeleteLed(int id, int? storeId = null)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            _logger.LogWarning("DeleteLed: client role {ClientRole} is not authorized to delete a led", clientRole);
            throw new UnauthorizedAccessException("You do not have permission to delete a led");
        }
        var ledToDelete = await _context.Leds.FindAsync(id);
        if (ledToDelete is null)
        {
            _logger.LogWarning("DeleteLed: led {LedId} not found", id);
            throw new KeyNotFoundException($"Led with id '{id}' not found");
        }
        if ((storeId is not null) && (ledToDelete.id_store != storeId))
        {
            _logger.LogWarning("DeleteLed: led {LedId} not found in store {StoreId}", id, storeId);
            throw new KeyNotFoundException($"Led with id '{id}' not found in store with id '{storeId}'");
        }
        _context.Leds.Remove(ledToDelete);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Led {LedId} deleted", id);
    }

    public async Task<ReadBulkLedDto> DeleteBulkLed(List<int> ids, int storeId)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            _logger.LogWarning("DeleteBulkLed: client role {ClientRole} is not authorized to delete leds", clientRole);
            throw new UnauthorizedAccessException("You do not have permission to delete leds");
        }
        var validQuery = new List<ReadLedDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var id in ids)
        {
            try
            {
                await DeleteLed(id, storeId);
                validQuery.Add(new ReadLedDto
                {
                    id_led = id
                });
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = new { id }
                });
            }
        }
        _logger.LogInformation("DeleteBulkLed: {SuccessCount} led(s) deleted, {ErrorCount} failed for store {StoreId}", validQuery.Count, errorQuery.Count, storeId);
        return new ReadBulkLedDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }

    public async Task ShowLedById(int storeId, int id, int redColor, int greenColor, int blueColor, int timeshow, int animation)
    {
        var ledDB = await _context.Leds
            .Where(led => led.id_store == storeId && led.id_led == id)
            .FirstOrDefaultAsync();
        if (ledDB is null)
        {
            _logger.LogWarning("ShowLedById: led {LedId} not found in store {StoreId}", id, storeId);
            throw new KeyNotFoundException($"Led with id '{id}' not found in store with id '{storeId}'");
        }
        if (!_mqttClient.IsConnected)
        {
            _logger.LogWarning("ShowLedById: MQTT client is not connected");
            throw new NotImplementedException("MQTT client is not connected");
        }
        var store = await _context.Stores.FindAsync(ledDB.id_store);
        if (store is null)
        {
            _logger.LogWarning("ShowLedById: store {StoreId} not found", ledDB.id_store);
            throw new KeyNotFoundException($"Store with id '{ledDB.id_store}' not found");
        }
        var topic = "electrostore/" + store.mqtt_name_store;
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(JsonSerializer.Serialize(new
            {
                leds = new[]
                {
                    new
                    {
                        index = ledDB.mqtt_led_id,
                        red = redColor,
                        blue = blueColor,
                        green = greenColor,
                        module = animation,
                        delay = timeshow
                    }
                }
            }))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
            .WithRetainFlag(false)
            .Build();
        await _mqttClient.PublishAsync(message);
        _logger.LogInformation("Led {LedId} shown in store {StoreId}", id, storeId);
    }

    public async Task ShowLedsByBox(int storeId, int boxId, int redColor, int greenColor, int blueColor, int timeshow, int animation)
    {
        // check if the store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == storeId))
        {
            _logger.LogWarning("ShowLedsByBox: store {StoreId} not found", storeId);
            throw new KeyNotFoundException($"Store with id '{storeId}' not found");
        }
        // check if the box exists
        if (!await _context.Boxs.AnyAsync(b => b.id_box == boxId && b.id_store == storeId))
        {
            _logger.LogWarning("ShowLedsByBox: box {BoxId} not found in store {StoreId}", boxId, storeId);
            throw new KeyNotFoundException($"Box with id '{boxId}' not found in store with id '{storeId}'");
        }
        var ledsDB = await _context.Leds
            .Join(_context.Boxs,
                led => new { led.id_store },
                box => new { box.id_store },
                (led, box) => new { led, box })
            .Where(x => x.box.id_box == boxId && x.led.id_store == storeId &&
                   x.led.x_led >= x.box.xstart_box && x.led.x_led <= x.box.xend_box &&
                     x.led.y_led >= x.box.ystart_box && x.led.y_led <= x.box.yend_box)
            .Select(x => x.led)
            .ToListAsync();
        if (ledsDB.Count == 0)
        {
            _logger.LogWarning("ShowLedsByBox: no leds found in store {StoreId} and box {BoxId}", storeId, boxId);
            throw new KeyNotFoundException($"No leds found in store with id '{storeId}' and box with id '{boxId}'");
        }
        if (!_mqttClient.IsConnected)
        {
            _logger.LogWarning("ShowLedsByBox: MQTT client is not connected");
            throw new NotImplementedException("MQTT client is not connected");
        }
        var store = await _context.Stores.FindAsync(storeId);
        if (store is null)
        {
            _logger.LogWarning("ShowLedsByBox: store {StoreId} not found", storeId);
            throw new KeyNotFoundException($"Store with id '{storeId}' not found");
        }
        var topic = "electrostore/" + store.mqtt_name_store;

        // sent led 10 per 10
        for (int i = 0; i <= ledsDB.Count; i+=numberLedSentPerMessage)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(JsonSerializer.Serialize(new
                {
                    leds = ledsDB
                        .Skip(i).Take(numberLedSentPerMessage)
                        .Select(led => new
                    {
                        index = led.mqtt_led_id,
                        red = redColor,
                        blue = blueColor,
                        green = greenColor,
                        module = animation,
                        delay = timeshow
                    })
                }))
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                .WithRetainFlag(false)
                .Build();
            await _mqttClient.PublishAsync(message);
        }
        _logger.LogInformation("Leds shown in box {BoxId} in store {StoreId} ({LedCount} led(s))", boxId, storeId, ledsDB.Count);
    }
}