using ElectrostoreAPI.Kafka.Producer;
using ElectrostoreAPI.Models;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ElectrostoreAPI.Grpc.Services;

public class StoreMqttGrpcService : StoresMqttGrpc.StoresMqttGrpcBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StoreMqttGrpcService> _logger;
    private readonly IKafkaProducerService _kafkaProducer;

    public StoreMqttGrpcService(
        ApplicationDbContext context,
        ILogger<StoreMqttGrpcService> logger,
        IKafkaProducerService kafkaProducer)
    {
        _context = context;
        _logger = logger;
        _kafkaProducer = kafkaProducer;
    }

    public override async Task<UpdateStoreMqttStatusReply> UpdateStoreMqttStatus(
        UpdateStoreMqttStatusRequest request, ServerCallContext context)
    {
        var stores = await _context.Stores
            .Where(s => s.mqtt_name_store == request.MqttNameStore)
            .ToListAsync(context.CancellationToken);

        if (stores.Count == 0)
        {
            _logger.LogWarning(
                "UpdateStoreMqttStatus: no store found with mqtt_name_store={Name}.", request.MqttNameStore);
            return new UpdateStoreMqttStatusReply { Success = false, StoreCount = 0 };
        }

        var now = DateTime.UtcNow;
        foreach (var store in stores)
        {
            store.is_mqtt_connected_store = request.IsMqttConnected;
            store.mqtt_last_seen_store    = now;
        }

        await _context.SaveChangesAsync(context.CancellationToken);
        return new UpdateStoreMqttStatusReply { Success = true, StoreCount = stores.Count };
    }
}
