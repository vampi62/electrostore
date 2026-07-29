using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Services.StoreService;
using Grpc.Core;

namespace ElectrostoreAPI.Grpc.Services;

public class StoreMqttGrpcService : StoresMqttGrpc.StoresMqttGrpcBase
{
    private readonly IStoreService _storeService;

    public StoreMqttGrpcService(
        IStoreService storeService)
    {
        _storeService = storeService;
    }

    public override async Task<UpdateStoreMqttStatusReply> UpdateStoreMqttStatus(
        UpdateStoreMqttStatusRequest request, ServerCallContext context)
    {
        var mqttStatusDto = new UpdateStoreMqttStatusDto
        {
            is_mqtt_connected_store = request.IsMqttConnected
        };
        var updatedCount = await _storeService.UpdateStoreMqttStatusByMqttNameAsync(
            request.MqttNameStore, mqttStatusDto, context.CancellationToken);
        if (updatedCount == 0)        {
            return new UpdateStoreMqttStatusReply
            {
                Success = false,
                StoreCount = 0
            };
        }
        return new UpdateStoreMqttStatusReply
        {
            Success = true,
            StoreCount = updatedCount
        };
    }
}
