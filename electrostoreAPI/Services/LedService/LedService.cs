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
    private readonly static int numberLedSentPerMessage = 10;

    public LedService(IMapper mapper, ApplicationDbContext context, IMqttClient mqttClient, ISessionService sessionService, IValidateStoreService validateStoreService)
    {
        _mapper = mapper;
        _context = context;
        _mqttClient = mqttClient;
        _sessionService = sessionService;
        _validateStoreService = validateStoreService;
    }

    public async Task<PaginatedResponseDto<ReadLedDto>> GetLedsByStoreId(int storeId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null)
    {
        // check if the store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == storeId))
        {
            throw new KeyNotFoundException($"Store with id '{storeId}' not found");
        }
        var query = _context.Leds.AsQueryable();
        var filterResult = default(Expression<Func<Leds, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { field = "id_store", search_type = "eq", value = storeId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<Leds>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<Leds>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { field = "id_led", order = "asc" };
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
                next_offset = offset + limit,
                has_more = await _context.Leds.Skip(offset + limit).AnyAsync(filterResult ?? (l => l.id_store == storeId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadLedDto> GetLedById(int id, int? storeId = null)
    {
        var led = await _context.Leds.FindAsync(id) ?? throw new KeyNotFoundException($"Led with id '{id}' not found");
        if ((storeId is not null) && (led.id_store != storeId))
        {
            throw new KeyNotFoundException($"Led with id '{id}' not found in store with id '{storeId}'");
        }
        return _mapper.Map<ReadLedDto>(led);
    }

    public async Task<ReadLedDto> CreateLed(CreateLedDto ledDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You do not have permission to create a led");
        }
        // check if store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == ledDto.id_store))
        {
            throw new KeyNotFoundException($"Store with id '{ledDto.id_store}' not found");
        }
        var newLed = _mapper.Map<Leds>(ledDto);
        var store = await _context.Stores.FindAsync(newLed.id_store) ?? throw new KeyNotFoundException($"Store with id '{newLed.id_store}' not found");
        _validateStoreService.ValidateLedPosition(newLed, store);
        _context.Leds.Add(newLed);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadLedDto>(newLed);
    }

    public async Task<ReadBulkLedDto> CreateBulkLed(List<CreateLedDto> ledsDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
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
                    reason = e.Message,
                    data = ledDto
                });
            }
        }
        return new ReadBulkLedDto
        {
            valide = validQuery,
            error = errorQuery
        };
    }

    public async Task<ReadLedDto> UpdateLed(int id, UpdateLedDto ledDto, int? storeId = null)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You do not have permission to update a led");
        }
        var ledToUpdate = await _context.Leds.FindAsync(id) ?? throw new KeyNotFoundException($"Led with id '{id}' not found");
        if ((storeId is not null) && (ledToUpdate.id_store != storeId))
        {
            throw new KeyNotFoundException($"Led with id '{id}' not found in store with id '{storeId}'");
        }
        await _validateStoreService.UpdateLedInformations(ledToUpdate, ledDto);
        var store = await _context.Stores.FindAsync(ledToUpdate.id_store) ?? throw new KeyNotFoundException($"Store with id '{ledToUpdate.id_store}' not found");
        _validateStoreService.ValidateLedPosition(ledToUpdate, store);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadLedDto>(ledToUpdate);
    }

    public async Task<ReadBulkLedDto> UpdateBulkLed(List<UpdateBulkLedByStoreDto> ledsDto, int storeId)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
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
                    mqtt_id_led = ledDto.mqtt_id_led
                };
                validQuery.Add(await UpdateLed(ledDto.id_led, ledDtoFull, storeId));
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    reason = e.Message,
                    data = ledDto
                });
            }
        }
        return new ReadBulkLedDto
        {
            valide = validQuery,
            error = errorQuery
        };
    }

    public async Task DeleteLed(int id, int? storeId = null)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You do not have permission to delete a led");
        }
        var ledToDelete = await _context.Leds.FindAsync(id) ?? throw new KeyNotFoundException($"Led with id '{id}' not found");
        if ((storeId is not null) && (ledToDelete.id_store != storeId))
        {
            throw new KeyNotFoundException($"Led with id '{id}' not found in store with id '{storeId}'");
        }
        _context.Leds.Remove(ledToDelete);
        await _context.SaveChangesAsync();
    }

    public async Task<ReadBulkLedDto> DeleteBulkLed(List<int> ids, int storeId)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
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
                    reason = e.Message,
                    data = new { id }
                });
            }
        }
        return new ReadBulkLedDto
        {
            valide = validQuery,
            error = errorQuery
        };
    }

    public async Task ShowLedById(int storeId, int id, int redColor, int greenColor, int blueColor, int timeshow, int animation)
    {
        var ledDB = await _context.Leds
            .Where(led => led.id_store == storeId && led.id_led == id)
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Led with id '{id}' not found in store with id '{storeId}'");
        if (!_mqttClient.IsConnected)
        {
            throw new NotImplementedException("MQTT client is not connected");
        }
        var store = await _context.Stores.FindAsync(ledDB.id_store) ?? throw new KeyNotFoundException($"Store with id '{ledDB.id_store}' not found");
        var topic = "electrostore/" + store.mqtt_name_store + "/leds";
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(JsonSerializer.Serialize(new
            {
                leds = new[]
                {
                    new
                    {
                        index = ledDB.mqtt_id_led,
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
    }

    public async Task ShowLedsByBox(int storeId, int boxId, int redColor, int greenColor, int blueColor, int timeshow, int animation)
    {
        var store = await _context.Stores.FindAsync(storeId) ?? throw new KeyNotFoundException($"Store with id '{storeId}' not found");
        // check if the box exists
        if (!await _context.Boxs.AnyAsync(b => b.id_box == boxId && b.id_store == storeId))
        {
            throw new KeyNotFoundException($"Box with id '{boxId}' not found in store with id '{storeId}'");
        }
        var ledsQuery = _context.Leds
            .Join(_context.Boxs,
                led => new { led.id_store },
                box => new { box.id_store },
                (led, box) => new { led, box })
            .Where(x => x.box.id_box == boxId && x.led.id_store == storeId);
        ledsQuery = store.position_mode_store == StorePositionMode.Border
            // border mode: leds sit along a store edge and light up when they share the box's level on that edge's axis
            ? ledsQuery.Where(x =>
                ((x.led.y_led == (int)LedBorderSide.Left || x.led.y_led == (int)LedBorderSide.Right) &&
                    x.led.x_led >= x.box.ystart_box && x.led.x_led <= x.box.yend_box) ||
                ((x.led.y_led == (int)LedBorderSide.Top || x.led.y_led == (int)LedBorderSide.Bottom) &&
                    x.led.x_led >= x.box.xstart_box && x.led.x_led <= x.box.xend_box))
            : ledsQuery.Where(x =>
                x.led.x_led >= x.box.xstart_box && x.led.x_led <= x.box.xend_box &&
                x.led.y_led >= x.box.ystart_box && x.led.y_led <= x.box.yend_box);
        var ledsDB = await ledsQuery.Select(x => x.led).ToListAsync();
        if (ledsDB.Count == 0)
        {
            throw new KeyNotFoundException($"No leds found in store with id '{storeId}' and box with id '{boxId}'");
        }
        if (!_mqttClient.IsConnected)
        {
            throw new NotImplementedException("MQTT client is not connected");
        }
        var topic = "electrostore/" + store.mqtt_name_store + "/leds";

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
                        index = led.mqtt_id_led,
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
    }
}