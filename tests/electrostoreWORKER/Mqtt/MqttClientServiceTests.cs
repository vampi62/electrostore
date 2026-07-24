using System.Reflection;
using ElectrostoreWORKER.Grpc;
using ElectrostoreWORKER.Mqtt;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElectrostoreWORKER.Tests.Mqtt;

public class MqttClientServiceTests
{
    // MqttClientService.ExecuteAsync opens a real TCP/MQTT connection and OnMessageReceivedAsync
    // takes an MQTTnet event-args type with no public constructor suited for unit tests, so
    // coverage focuses on UpdateStoreMqttStatusAsync, the private method that owns the gRPC call.
    private readonly Mock<StoresMqttGrpc.StoresMqttGrpcClient> _grpcClient = new();
    private readonly Mock<ILogger<MqttClientService>> _logger = new();

    private MqttClientService CreateService()
    {
        var configuration = new ConfigurationBuilder().Build();
        return new MqttClientService(configuration, _logger.Object, _grpcClient.Object);
    }

    private static AsyncUnaryCall<TResponse> CreateAsyncUnaryCall<TResponse>(TResponse response)
    {
        return new AsyncUnaryCall<TResponse>(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
    }

    private static Task UpdateStoreMqttStatusAsync(MqttClientService service, string mqttNameStore, bool isConnected)
    {
        var method = typeof(MqttClientService).GetMethod("UpdateStoreMqttStatusAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("UpdateStoreMqttStatusAsync method not found");
        return (Task)method.Invoke(service, new object[] { mqttNameStore, isConnected })!;
    }

    [Fact]
    public async Task UpdateStoreMqttStatusAsync_ShouldCallApi_WithMappedFields()
    {
        // Arrange
        var service = CreateService();
        _grpcClient
            .Setup(c => c.UpdateStoreMqttStatusAsync(It.IsAny<UpdateStoreMqttStatusRequest>(), null, null, default))
            .Returns(CreateAsyncUnaryCall(new UpdateStoreMqttStatusReply { Success = true, StoreCount = 2 }));

        // Act
        await UpdateStoreMqttStatusAsync(service, "store-1", true);

        // Assert
        _grpcClient.Verify(c => c.UpdateStoreMqttStatusAsync(
            It.Is<UpdateStoreMqttStatusRequest>(r => r.MqttNameStore == "store-1" && r.IsMqttConnected),
            null, null, default), Times.Once);
    }

    [Fact]
    public async Task UpdateStoreMqttStatusAsync_ShouldNotThrow_WhenNoStoreFound()
    {
        // Arrange
        var service = CreateService();
        _grpcClient
            .Setup(c => c.UpdateStoreMqttStatusAsync(It.IsAny<UpdateStoreMqttStatusRequest>(), null, null, default))
            .Returns(CreateAsyncUnaryCall(new UpdateStoreMqttStatusReply { Success = false, StoreCount = 0 }));

        // Act
        var exception = await Record.ExceptionAsync(() => UpdateStoreMqttStatusAsync(service, "unknown-store", false));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task UpdateStoreMqttStatusAsync_ShouldNotThrow_WhenGrpcCallFails()
    {
        // Arrange
        var service = CreateService();
        _grpcClient
            .Setup(c => c.UpdateStoreMqttStatusAsync(It.IsAny<UpdateStoreMqttStatusRequest>(), null, null, default))
            .Throws(new RpcException(new Status(StatusCode.Unavailable, "down")));

        // Act
        var exception = await Record.ExceptionAsync(() => UpdateStoreMqttStatusAsync(service, "store-1", true));

        // Assert
        Assert.Null(exception);
    }
}
