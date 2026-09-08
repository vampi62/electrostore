using System.Net;
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
    // OnMessageReceivedAsync and UpdateStoreMqttStatusAsync, the private methods that own the topic
    // parsing and the gRPC call, are covered via MqttApplicationMessageReceivedEventArgs's public
    // constructor. ExecuteAsync is covered via a mocked IMqttClient injected through the
    // CreateMqttClient() test seam, since it otherwise opens a real TCP/MQTT connection.
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

    // ---- ExecuteAsync ----

    private sealed class TestableMqttClientService(
        IConfiguration configuration,
        ILogger<MqttClientService> logger,
        StoresMqttGrpc.StoresMqttGrpcClient grpcClient,
        IMqttClient mqttClient) : MqttClientService(configuration, logger, grpcClient)
    {
        protected override IMqttClient CreateMqttClient() => mqttClient;

        public Task RunExecuteAsync(CancellationToken ct) => ExecuteAsync(ct);
    }

    private static MqttClientSubscribeResult CreateSubscribeResult() =>
        new(0, Array.Empty<MqttClientSubscribeResultItem>(), null!, Array.Empty<MqttUserProperty>());

    [Fact]
    public async Task ExecuteAsync_ShouldConnectSubscribeAndDisconnect_WhenCancelledWhileConnected()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var mqttClient = new Mock<IMqttClient>();
        mqttClient
            .Setup(c => c.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MqttClientConnectResult());
        mqttClient
            .Setup(c => c.SubscribeAsync(It.IsAny<MqttClientSubscribeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSubscribeResult());
        mqttClient.SetupGet(c => c.IsConnected).Returns(true);
        mqttClient
            .Setup(c => c.DisconnectAsync(It.IsAny<MqttClientDisconnectOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = new TestableMqttClientService(configuration, _logger.Object, _grpcClient.Object, mqttClient.Object);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(30));

        // Act - cancellation fires while waiting in the "connected" inner loop, which is caught
        // and turned into a clean shutdown (including the final disconnect).
        await service.RunExecuteAsync(cts.Token);

        // Assert
        mqttClient.Verify(c => c.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        mqttClient.Verify(c => c.SubscribeAsync(It.IsAny<MqttClientSubscribeOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        mqttClient.Verify(c => c.DisconnectAsync(It.IsAny<MqttClientDisconnectOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPropagateCancellation_WhenCancelledBeforeConnectionEstablished()
    {
        // Arrange - IsConnected stays false, so the inner loop never runs and cancellation is
        // observed while waiting on the outer retry delay, which is not caught inside the method.
        var configuration = new ConfigurationBuilder().Build();
        var mqttClient = new Mock<IMqttClient>();
        mqttClient
            .Setup(c => c.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MqttClientConnectResult());
        mqttClient
            .Setup(c => c.SubscribeAsync(It.IsAny<MqttClientSubscribeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSubscribeResult());
        mqttClient.SetupGet(c => c.IsConnected).Returns(false);
        var service = new TestableMqttClientService(configuration, _logger.Object, _grpcClient.Object, mqttClient.Object);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(30));

        // Act
        var exception = await Record.ExceptionAsync(() => service.RunExecuteAsync(cts.Token));

        // Assert
        Assert.IsAssignableFrom<OperationCanceledException>(exception);
        mqttClient.Verify(c => c.SubscribeAsync(It.IsAny<MqttClientSubscribeOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        mqttClient.Verify(c => c.DisconnectAsync(It.IsAny<MqttClientDisconnectOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLogAndRetry_WhenConnectThrowsGenericException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var mqttClient = new Mock<IMqttClient>();
        mqttClient
            .Setup(c => c.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("connection refused"));
        mqttClient.SetupGet(c => c.IsConnected).Returns(false);
        var service = new TestableMqttClientService(configuration, _logger.Object, _grpcClient.Object, mqttClient.Object);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(30));

        // Act
        var exception = await Record.ExceptionAsync(() => service.RunExecuteAsync(cts.Token));

        // Assert
        Assert.IsAssignableFrom<OperationCanceledException>(exception);
        mqttClient.Verify(c => c.SubscribeAsync(It.IsAny<MqttClientSubscribeOptions>(), It.IsAny<CancellationToken>()), Times.Never);
        _logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<InvalidOperationException>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldBuildOptionsWithCredentialsAndConfiguredHost_WhenProvided()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mqtt:Host"] = "custom-host",
                ["Mqtt:Port"] = "not-a-number",
                ["Mqtt:Username"] = "user1",
                ["Mqtt:Password"] = "pass1",
                ["Mqtt:ClientId"] = "worker-test",
            })
            .Build();
        var mqttClient = new Mock<IMqttClient>();
        MqttClientOptions? capturedOptions = null;
        mqttClient
            .Setup(c => c.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()))
            .Callback<MqttClientOptions, CancellationToken>((options, _) => capturedOptions = options)
            .ThrowsAsync(new InvalidOperationException("stop after capture"));
        mqttClient.SetupGet(c => c.IsConnected).Returns(false);
        var service = new TestableMqttClientService(configuration, _logger.Object, _grpcClient.Object, mqttClient.Object);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(30));

        // Act
        await Record.ExceptionAsync(() => service.RunExecuteAsync(cts.Token));

        // Assert - invalid port string falls back to the 1883 default, and credentials are only
        // attached when a username is configured.
        Assert.NotNull(capturedOptions);
        Assert.Equal("worker-test", capturedOptions!.ClientId);
        Assert.NotNull(capturedOptions.Credentials);
        var tcpOptions = Assert.IsType<MqttClientTcpOptions>(capturedOptions.ChannelOptions);
        var endpoint = Assert.IsType<DnsEndPoint>(tcpOptions.RemoteEndpoint);
        Assert.Equal("custom-host", endpoint.Host);
        Assert.Equal(1883, endpoint.Port);
    }
}
