using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Grpc;
using ElectrostoreAPI.Grpc.Services;
using ElectrostoreAPI.Services.StoreService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Grpc.Services;

public class StoreMqttGrpcServiceTests
{
    private readonly Mock<IStoreService> _storeService;
    private readonly StoreMqttGrpcService _service;

    public StoreMqttGrpcServiceTests()
    {
        _storeService = new Mock<IStoreService>();
        _service = new StoreMqttGrpcService(_storeService.Object, new LoggerFactory().CreateLogger<StoreMqttGrpcService>());
    }

    [Fact]
    public async Task UpdateStoreMqttStatus_ShouldReturnFailureAndZeroCount_WhenNoStoreUpdated()
    {
        // Arrange
        var request = new UpdateStoreMqttStatusRequest { MqttNameStore = "unknown", IsMqttConnected = true };
        _storeService.Setup(s => s.UpdateStoreMqttStatusByMqttNameAsync("unknown", It.IsAny<UpdateStoreMqttStatusDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var reply = await _service.UpdateStoreMqttStatus(request, TestServerCallContext.Create());

        // Assert
        Assert.False(reply.Success);
        Assert.Equal(0, reply.StoreCount);
    }

    [Fact]
    public async Task UpdateStoreMqttStatus_ShouldReturnSuccessAndCount_WhenStoresUpdated()
    {
        // Arrange
        var request = new UpdateStoreMqttStatusRequest { MqttNameStore = "store1", IsMqttConnected = true };
        UpdateStoreMqttStatusDto? captured = null;
        _storeService.Setup(s => s.UpdateStoreMqttStatusByMqttNameAsync("store1", It.IsAny<UpdateStoreMqttStatusDto>(), It.IsAny<CancellationToken>()))
            .Callback<string, UpdateStoreMqttStatusDto, CancellationToken>((_, dto, _) => captured = dto)
            .ReturnsAsync(2);

        // Act
        var reply = await _service.UpdateStoreMqttStatus(request, TestServerCallContext.Create());

        // Assert
        Assert.True(reply.Success);
        Assert.Equal(2, reply.StoreCount);
        Assert.NotNull(captured);
        Assert.True(captured!.is_mqtt_connected_store);
    }

    [Fact]
    public async Task UpdateStoreMqttStatus_ShouldForwardDisconnectedState()
    {
        // Arrange
        var request = new UpdateStoreMqttStatusRequest { MqttNameStore = "store1", IsMqttConnected = false };
        UpdateStoreMqttStatusDto? captured = null;
        _storeService.Setup(s => s.UpdateStoreMqttStatusByMqttNameAsync("store1", It.IsAny<UpdateStoreMqttStatusDto>(), It.IsAny<CancellationToken>()))
            .Callback<string, UpdateStoreMqttStatusDto, CancellationToken>((_, dto, _) => captured = dto)
            .ReturnsAsync(1);

        // Act
        await _service.UpdateStoreMqttStatus(request, TestServerCallContext.Create());

        // Assert
        Assert.NotNull(captured);
        Assert.False(captured!.is_mqtt_connected_store);
    }
}
