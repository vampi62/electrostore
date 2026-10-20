using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Grpc;
using ElectrostoreAPI.Grpc.Services;
using ElectrostoreAPI.Services.StoreService;
using ElectrostoreAPI.Tests.Utils;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElectrostoreAPI.Tests.Grpc;

public class StoreMqttGrpcServiceTests
{
    private readonly Mock<IStoreService> _storeService = new();
    private readonly Mock<ILogger<StoreMqttGrpcService>> _logger = new();

    private StoreMqttGrpcService CreateService() => new(_storeService.Object, _logger.Object);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UpdateStoreMqttStatus_ShouldReturnSuccessAndCount_WhenStoresUpdated(bool connected)
    {
        _storeService
            .Setup(s => s.UpdateStoreMqttStatusByMqttNameAsync("store-1", It.IsAny<UpdateStoreMqttStatusDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        var service = CreateService();

        var reply = await service.UpdateStoreMqttStatus(
            new UpdateStoreMqttStatusRequest { MqttNameStore = "store-1", IsMqttConnected = connected },
            TestServerCallContext.Create());

        Assert.True(reply.Success);
        Assert.Equal(2, reply.StoreCount);
        _storeService.Verify(s => s.UpdateStoreMqttStatusByMqttNameAsync(
            "store-1",
            It.Is<UpdateStoreMqttStatusDto>(d => d.is_mqtt_connected_store == connected),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStoreMqttStatus_ShouldReturnFailure_WhenNoStoreMatches()
    {
        _storeService
            .Setup(s => s.UpdateStoreMqttStatusByMqttNameAsync(It.IsAny<string>(), It.IsAny<UpdateStoreMqttStatusDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        var service = CreateService();

        var reply = await service.UpdateStoreMqttStatus(
            new UpdateStoreMqttStatusRequest { MqttNameStore = "unknown", IsMqttConnected = true },
            TestServerCallContext.Create());

        Assert.False(reply.Success);
        Assert.Equal(0, reply.StoreCount);
    }
}
