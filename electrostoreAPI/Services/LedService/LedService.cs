using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using electrostore.Enums;
using MQTTnet;
using MQTTnet.Protocol;
using System.Text.Json;
using electrostore.Services.SessionService;

namespace electrostore.Services.LedService;

public class LedService : ILedService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IMqttClient _mqttClient;
    private readonly ISessionService _sessionService;

    public LedService(IMapper mapper, ApplicationDbContext context, IMqttClient mqttClient, ISessionService sessionService)
    {
        _mapper = mapper;
        _context = context;
        _mqttClient = mqttClient;
        _sessionService = sessionService;
    }

    public async Task<IEnumerable<ReadLedDto>> GetLedsByStoreId(int storeId, int limit = 100, int offset = 0)
    {
        // check if the store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == storeId))
        {
            throw new KeyNotFoundException($"Store with id {storeId} not found");
        }
        var query = _context.Leds.AsQueryable();
        query = query.Where(l => l.id_store == storeId);
        query = query.Skip(offset).Take(limit);
        query = query.OrderBy(l => l.id_led);
        var led = await query.ToListAsync();
        return _mapper.Map<List<ReadLedDto>>(led);
    }

    public async Task<int> GetLedsCountByStoreId(int storeId)
    {
        // check if the store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == storeId))
        {
            throw new KeyNotFoundException($"Store with id {storeId} not found");
        }
        return await _context.Leds
            .Where(led => led.id_store == storeId)
            .CountAsync();
    }

    public async Task<ReadLedDto> GetLedById(int id, int? storeId = null)
    {
        var led = await _context.Leds.FindAsync(id) ?? throw new KeyNotFoundException($"Led with id {id} not found");
        if ((storeId is not null) && (led.id_store != storeId))
        {
            throw new KeyNotFoundException($"Led with id {id} not found in store with id {storeId}");
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
            throw new KeyNotFoundException($"Store with id {ledDto.id_store} not found");
        }
        var newLed = _mapper.Map<Leds>(ledDto);
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
                    Reason = e.Message,
                    Data = ledDto
                });
            }
        }
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
            throw new UnauthorizedAccessException("You do not have permission to update a led");
        }
        var ledToUpdate = await _context.Leds.FindAsync(id) ?? throw new KeyNotFoundException($"Led with id {id} not found");
        if ((storeId is not null) && (ledToUpdate.id_store != storeId))
        {
            throw new KeyNotFoundException($"Led with id {id} not found in store with id {storeId}");
        }
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
            throw new UnauthorizedAccessException("You do not have permission to delete a led");
        }
        var ledToDelete = await _context.Leds.FindAsync(id) ?? throw new KeyNotFoundException($"Led with id {id} not found");
        if ((storeId is not null) && (ledToDelete.id_store != storeId))
        {
            throw new KeyNotFoundException($"Led with id {id} not found in store with id {storeId}");
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
                    Reason = e.Message,
                    Data = new { id }
                });
            }
        }
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
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Led with id {id} not found in store with id {storeId}");
        if (!_mqttClient.IsConnected)
        {
            throw new NotImplementedException("MQTT client is not connected");
        }
        var store = await _context.Stores.FindAsync(ledDB.id_store) ?? throw new KeyNotFoundException($"Store with id {ledDB.id_store} not found");
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
    }

    public async Task ShowLedsByBox(int storeId, int boxId, int redColor, int greenColor, int blueColor, int timeshow, int animation)
    {
        // check if the store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == storeId))
        {
            throw new KeyNotFoundException($"Store with id {storeId} not found");
        }
        // check if the box exists
        if (!await _context.Boxs.AnyAsync(b => b.id_box == boxId && b.id_store == storeId))
        {
            throw new KeyNotFoundException($"Box with id {boxId} not found in store with id {storeId}");
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
        if (!ledsDB.Any())
        {
            throw new KeyNotFoundException($"No leds found in store with id {storeId} and box with id {boxId}");
        }
        if (!_mqttClient.IsConnected)
        {
            throw new NotImplementedException("MQTT client is not connected");
        }
        var store = await _context.Stores.FindAsync(storeId) ?? throw new KeyNotFoundException($"Store with id {storeId} not found");
        var topic = "electrostore/" + store.mqtt_name_store;
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(JsonSerializer.Serialize(new
            {
                leds = ledsDB.Select(led => new
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
}