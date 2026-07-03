using ElectrostoreWORKER.Grpc;
using MQTTnet;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace ElectrostoreWORKER.Mqtt;

public class MqttClientService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MqttClientService> _logger;
    private readonly StoresMqttGrpc.StoresMqttGrpcClient _grpcClient;

    public MqttClientService(
        IConfiguration configuration,
        ILogger<MqttClientService> logger,
        StoresMqttGrpc.StoresMqttGrpcClient grpcClient)
    {
        _configuration = configuration;
        _logger        = logger;
        _grpcClient    = grpcClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var host = _configuration["Mqtt:Host"] ?? "mosquitto";
        var portStr = _configuration["Mqtt:Port"] ?? "1883";
        var username = _configuration["Mqtt:Username"] ?? string.Empty;
        var password = _configuration["Mqtt:Password"] ?? string.Empty;
        var clientId = _configuration["Mqtt:ClientId"] ?? $"worker-{Guid.NewGuid():N}";

        if (!int.TryParse(portStr, out var port)) port = 1883;

        var factory = new MqttClientFactory();
        using var client = factory.CreateMqttClient();

        client.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

        var optionsBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .WithClientId(clientId)
            .WithCleanSession();

        if (!string.IsNullOrWhiteSpace(username))
            optionsBuilder = optionsBuilder.WithCredentials(username, password);

        var options = optionsBuilder.Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("MQTT: connecting to {Host}:{Port}...", host, port);
                await client.ConnectAsync(options, stoppingToken);

                var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter("electrostore/+/status", MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await client.SubscribeAsync(subscribeOptions, stoppingToken);
                _logger.LogInformation("MQTT: connected and subscribed.");

                // Wait until disconnected or stopped
                while (client.IsConnected && !stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MQTT: connection error. Retrying in 10 s.");
            }

            if (!stoppingToken.IsCancellationRequested)
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }

        if (client.IsConnected)
            await client.DisconnectAsync(cancellationToken: stoppingToken);
    }

    private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic   = e.ApplicationMessage.Topic;
        var payload = e.ApplicationMessage.ConvertPayloadToString();

        _logger.LogDebug("MQTT received - topic={Topic}, payload={Payload}", topic, payload);

        // Expected format: electrostore/{mqtt_name_store}/status
        const string prefix = "electrostore/";
        const string suffix = "/status";

        if (topic.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && topic.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            var mqttName = topic[prefix.Length..^suffix.Length];

            if (!string.IsNullOrWhiteSpace(mqttName))
            {
                var isConnected = payload?.Trim() is "1" or "true" or "online";
                _ = UpdateStoreMqttStatusAsync(mqttName, isConnected);
            }
        }

        return Task.CompletedTask;
    }

    private async Task UpdateStoreMqttStatusAsync(string mqttNameStore, bool isConnected)
    {
        try
        {
            var reply = await _grpcClient.UpdateStoreMqttStatusAsync(
                new UpdateStoreMqttStatusRequest
                {
                    MqttNameStore   = mqttNameStore,
                    IsMqttConnected = isConnected
                });

            if (reply.Success)
                _logger.LogInformation(
                    "MQTT: store(s) '{Name}' updated - connected={Connected}, count={Count}",
                    mqttNameStore, isConnected, reply.StoreCount);
            else
                _logger.LogWarning(
                    "MQTT: no store found for mqtt_name_store='{Name}'.", mqttNameStore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MQTT: gRPC error in UpdateStoreMqttStatus (name={Name}).", mqttNameStore);
        }
    }
}