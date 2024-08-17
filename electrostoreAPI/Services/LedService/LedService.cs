using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.LedService;

public class LedService : ILedService
{
    private readonly ApplicationDbContext _context;
    private readonly IMqttClient _mqttClient;

    public LedService(ApplicationDbContext context, IMqttClient mqttClient)
    {
        _context = context;
        _mqttClient = mqttClient;
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

    public async Task<ActionResult<IEnumerable<ReadLedDto>>> GetLedsByStoreId(int storeId, int limit = 100, int offset = 0)
    {
        // check if the store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == storeId))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_store = new string[] { "Store not found" } }});
        }
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

    public async Task<ActionResult<IEnumerable<ReadLedDto>>> GetLedsByStoreIdAndPosition(int storeId, int xmin, int xmax, int ymin, int ymax)
    {
        // check if the store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == storeId))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_store = new string[] { "Store not found" } }});
        }
        return await _context.Leds
            .Where(led => led.x_led >= xmin && led.x_led <= xmax && led.y_led >= ymin && led.y_led <= ymax && led.id_store == storeId)
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

    public async Task<ActionResult<ReadLedDto>> GetLedById(int id, int? storeId = null)
    {
        var led = await _context.Leds.FindAsync(id);
        if (led == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_led = new string[] { "Led not found" } }});
        }
        if (led.id_store != storeId)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_store = new string[] { "Led does not belong to this store" } }});
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

    public async Task<ActionResult<ReadLedDto>> CreateLed(CreateLedDto ledDto)
    {
        if (ledDto.x_led < 0 || ledDto.y_led < 0 || ledDto.mqtt_led_id < 0)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { x_led = new string[] { "Coordinates must be positive" }, y_led = new string[] { "Coordinates must be positive" }, mqtt_led_id = new string[] { "Mqtt id must be positive" } }});
        }

        // check if store exists
        if (!await _context.Stores.AnyAsync(s => s.id_store == ledDto.id_store))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_store = new string[] { "Store not found" } }});
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

    public async Task<ActionResult<ReadLedDto>> UpdateLed(int id, UpdateLedDto ledDto, int? storeId = null)
    {
        var ledToUpdate = await _context.Leds.FindAsync(id);
        if (ledToUpdate == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_led = new string[] { "Led not found" } }});
        }
        if (ledToUpdate.id_store != storeId)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_store = new string[] { "Led does not belong to this store" } }});
        }

        if (ledDto.x_led != null)
        {
            if (ledDto.x_led < 0)
            {
                return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { x_led = new string[] { "Coordinates must be positive" } }});
            }
            ledToUpdate.x_led = ledDto.x_led.Value;
        }

        if (ledDto.y_led != null)
        {
            if (ledDto.y_led < 0)
            {
                return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { y_led = new string[] { "Coordinates must be positive" } }});
            }
            ledToUpdate.y_led = ledDto.y_led.Value;
        }

        if (ledDto.new_id_store != null)
        {
            // check if store exists
            if (!await _context.Stores.AnyAsync(s => s.id_store == ledDto.new_id_store))
            {
                return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_store = new string[] { "Store not found" } }});
            }
            ledToUpdate.id_store = ledDto.new_id_store.Value;
        }

        if (ledDto.mqtt_led_id != null)
        {
            if (ledDto.mqtt_led_id < 0)
            {
                return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { mqtt_led_id = new string[] { "Mqtt id must be positive" } }});
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

    public async Task<IActionResult> DeleteLed(int id, int? storeId = null)
    {
        var ledToDelete = await _context.Leds.FindAsync(id);
        if (ledToDelete == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_led = new string[] { "Led not found" } }});
        }
        if (ledToDelete.id_store != storeId)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_store = new string[] { "Led does not belong to this store" } }});
        }
        _context.Leds.Remove(ledToDelete);
        await _context.SaveChangesAsync();
        return new OkResult();
    }

    public async Task<IActionResult> ShowLed(ReadLedDto ledDB, int redColor, int greenColor, int blueColor, int timeshow, int animation)
    {
        if (!_mqttClient.IsConnected)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { mqtt = new string[] { "MQTT client is not connected" } }});
        }
        var store = await _context.Stores.FindAsync(ledDB.id_store);
        if (store == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_store = new string[] { "Store not found" } }});
        }
        var topic = store.mqtt_name_store;
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
        return new OkResult();
    }

    public async Task<IActionResult> ShowLeds(IEnumerable<ReadLedDto> ledsDB, int redColor, int greenColor, int blueColor, int timeshow, int animation)
    {
        if (!_mqttClient.IsConnected)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { mqtt = new string[] { "MQTT client is not connected" } }});
        }
        var store = await _context.Stores.FindAsync(ledsDB.First().id_store);
        if (store == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_store = new string[] { "Store not found" } }});
        }
        var topic = store.mqtt_name_store;
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
        return new OkResult();
    }

}