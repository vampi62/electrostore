using System.Reflection;
using System.Text;
using ElectrostoreWORKER.Grpc;
using ElectrostoreWORKER.Mqtt;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MQTTnet;
using MQTTnet.Packets;
using Xunit;

namespace ElectrostoreWORKER.Tests.Mqtt;

public class MqttClientServiceTests
{
    // MqttClientService.ExecuteAsync opens a real TCP/MQTT connection, so it isn't unit-tested here.
    // OnMessageReceivedAsync and UpdateStoreMqttStatusAsync, the private methods that own the topic
    // parsing and the gRPC call, are covered via MqttApplicationMessageReceivedEventArgs's public
    // constructor.
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

    private static MqttApplicationMessageReceivedEventArgs CreateReceivedEventArgs(string topic, string? payload)
    {
        var message = new MqttApplicationMessage
        {
            Topic = topic,
            PayloadSegment = payload is null ? default : new ArraySegment<byte>(Encoding.UTF8.GetBytes(payload))
        };
        return new MqttApplicationMessageReceivedEventArgs("test-client", message, new MqttPublishPacket(), (_, _) => Task.CompletedTask);
    }

    private static Task OnMessageReceivedAsync(MqttClientService service, MqttApplicationMessageReceivedEventArgs args)
    {
        var method = typeof(MqttClientService).GetMethod("OnMessageReceivedAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("OnMessageReceivedAsync method not found");
        return (Task)method.Invoke(service, new object[] { args })!;
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

    // ---- OnMessageReceivedAsync ----

    [Theory]
    [InlineData("1")]
    [InlineData("true")]
    [InlineData("online")]
    public async Task OnMessageReceivedAsync_ShouldReportConnected_ForRecognizedOnlinePayloads(string payload)
    {
        // Arrange
        var service = CreateService();
        var tcs = new TaskCompletionSource();
        _grpcClient
            .Setup(c => c.UpdateStoreMqttStatusAsync(It.IsAny<UpdateStoreMqttStatusRequest>(), null, null, default))
            .Callback(() => tcs.TrySetResult())
            .Returns(CreateAsyncUnaryCall(new UpdateStoreMqttStatusReply { Success = true, StoreCount = 1 }));
        var args = CreateReceivedEventArgs("electrostore/store-1/status", payload);

        // Act
        await OnMessageReceivedAsync(service, args);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        _grpcClient.Verify(c => c.UpdateStoreMqttStatusAsync(
            It.Is<UpdateStoreMqttStatusRequest>(r => r.MqttNameStore == "store-1" && r.IsMqttConnected),
            null, null, default), Times.Once);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("offline")]
    [InlineData("")]
    public async Task OnMessageReceivedAsync_ShouldReportDisconnected_ForOtherPayloads(string payload)
    {
        // Arrange
        var service = CreateService();
        var tcs = new TaskCompletionSource();
        _grpcClient
            .Setup(c => c.UpdateStoreMqttStatusAsync(It.IsAny<UpdateStoreMqttStatusRequest>(), null, null, default))
            .Callback(() => tcs.TrySetResult())
            .Returns(CreateAsyncUnaryCall(new UpdateStoreMqttStatusReply { Success = true, StoreCount = 1 }));
        var args = CreateReceivedEventArgs("electrostore/store-2/status", payload);

        // Act
        await OnMessageReceivedAsync(service, args);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        _grpcClient.Verify(c => c.UpdateStoreMqttStatusAsync(
            It.Is<UpdateStoreMqttStatusRequest>(r => r.MqttNameStore == "store-2" && !r.IsMqttConnected),
            null, null, default), Times.Once);
    }

    [Theory]
    [InlineData("not/a/status/topic")]
    [InlineData("electrostore/store-1/other")]
    [InlineData("other/electrostore/status")]
    public async Task OnMessageReceivedAsync_ShouldNotCallApi_WhenTopicDoesNotMatchExpectedShape(string topic)
    {
        // Arrange
        var service = CreateService();
        var args = CreateReceivedEventArgs(topic, "online");

        // Act
        await OnMessageReceivedAsync(service, args);
        await Task.Delay(50);

        // Assert
        _grpcClient.Verify(c => c.UpdateStoreMqttStatusAsync(It.IsAny<UpdateStoreMqttStatusRequest>(), null, null, default), Times.Never);
    }

    [Fact]
    public async Task OnMessageReceivedAsync_ShouldNotCallApi_WhenMqttNameIsEmpty()
    {
        // Arrange
        var service = CreateService();
        var args = CreateReceivedEventArgs("electrostore//status", "online");

        // Act
        await OnMessageReceivedAsync(service, args);
        await Task.Delay(50);

        // Assert
        _grpcClient.Verify(c => c.UpdateStoreMqttStatusAsync(It.IsAny<UpdateStoreMqttStatusRequest>(), null, null, default), Times.Never);
    }
}
