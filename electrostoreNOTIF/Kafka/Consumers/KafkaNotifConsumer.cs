using System.Text.Json;
using Confluent.Kafka;
using ElectrostoreNOTIF.Kafka.Messages;
using ElectrostoreNOTIF.Services.EmailSenderService;
using ElectrostoreNOTIF.Services.WebPushService;
using ElectrostoreNOTIF.Grpc;
using Grpc.Core;

namespace ElectrostoreNOTIF.Kafka.Consumers;

public class KafkaNotifConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly IConfiguration _configuration;
    private readonly IEmailSenderService _email;
    private readonly IWebPushService _webPush;
    private readonly UsersGrpc.UsersGrpcClient _userResolver;
    private readonly ILogger<KafkaNotifConsumer> _logger;

    public KafkaNotifConsumer(
        IConfiguration configuration,
        IEmailSenderService email,
        IWebPushService webPush,
        UsersGrpc.UsersGrpcClient userResolver,
        ILogger<KafkaNotifConsumer> logger)
    {
        _configuration = configuration;
        _email = email;
        _webPush = webPush;
        _userResolver = userResolver;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "kafka:9092";
        var groupId = _configuration["Kafka:ConsumerGroupId"] ?? "notif-service";
        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnablePartitionEof  = true,
            SessionTimeoutMs = 60_000,
            HeartbeatIntervalMs = 15_000,
        };
        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) =>
                _logger.LogError(
                    "[Kafka] Broker error | Code: {Code} | Reason: {Reason} | Fatal: {Fatal}",
                    e.Code, e.Reason, e.IsFatal))
            .SetPartitionsAssignedHandler((_, partitions) =>
                _logger.LogInformation(
                    "[Kafka] Partitions assigned → {Parts}",
                    string.Join(", ", partitions.Select(p => $"{p.Topic}[{p.Partition}]"))))
            .SetPartitionsRevokedHandler((_, partitions) =>
                _logger.LogWarning(
                    "[Kafka] Partitions revoked → {Parts}",
                    string.Join(", ", partitions.Select(p => $"{p.Topic}[{p.Partition}]"))))
            .Build();
        consumer.Subscribe("notification-requests");
        _logger.LogInformation(
            "KafkaNotifConsumer started (group={Group}, servers={Servers})",
            groupId, bootstrapServers);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result = null;
                try
                {
                    result = consumer.Consume(TimeSpan.FromSeconds(2));
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error");
                    continue;
                }
                if (result is null || result.IsPartitionEOF)
                {
                    continue;
                }
                if (result?.Message?.Value is null)
                {
                    continue;
                }
                NotificationMessage? msg;
                try
                {
                    msg = JsonSerializer.Deserialize<NotificationMessage>(result.Message.Value, JsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Invalid Kafka message (JSON) — offset {Offset}", result.Offset);
                    consumer.Commit(result);
                    continue;
                }
                if (msg is null)
                {
                    consumer.Commit(result);
                    continue;
                }
                var dispatched = false;
                try
                {
                    await DispatchAsync(msg, stoppingToken);
                    dispatched = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error dispatching notification message for offset {Offset}", result.Offset);
                }
                if (dispatched)
                {
                    consumer.Commit(result);
                }
            }
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("KafkaNotifConsumer stopped");
        }
    }

    private async Task DispatchAsync(NotificationMessage msg, CancellationToken ct)
    {
        string? emailAddress = msg.RecipientEmail;
        if (string.IsNullOrEmpty(emailAddress) && msg.RecipientUserId.HasValue)
        {
            try
            {
                var reply = await _userResolver.GetUserInfoAsync(
                    new GetUserInfoRequest { UserId = msg.RecipientUserId.Value },
                    cancellationToken: ct);
                if (!reply.Found)
                {
                    _logger.LogWarning("User {Id} not found in API", msg.RecipientUserId.Value);
                    return;
                }
                emailAddress = reply.Email;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "Error while calling API to get user info (userId={UserId})", msg.RecipientUserId.Value);
                return;
            }
        }
        foreach (var type in msg.Types)
        {
            switch (type.ToLowerInvariant())
            {
                case "email":
                    if (!string.IsNullOrEmpty(emailAddress))
                    {
                        await _email.SendAsync(emailAddress, msg.Subject, msg.Body);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Cannot send email notification — no email address for user ID {UserId}",
                            msg.RecipientUserId);
                    }
                    break;
                case "web_push":
                    if (msg.RecipientUserId.HasValue)
                    {
                        GetUserPushSubscriptionsReply? pushSubs;
                        try
                        {
                            pushSubs = await _userResolver.GetUserPushSubscriptionsAsync(
                                new GetUserPushSubscriptionsRequest { UserId = msg.RecipientUserId.Value },
                                cancellationToken: ct);
                        }
                        catch (RpcException ex)
                        {
                            _logger.LogError(ex, "Error fetching push subscriptions for user {UserId}", msg.RecipientUserId.Value);
                            break;
                        }
                        foreach (var sub in pushSubs.Subscriptions)
                        {
                            try
                            {
                                await _webPush.SendAsync(sub.Endpoint, sub.P256Dh, sub.Auth, msg.Title, msg.Body, msg.PushData);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to send web push to subscription {Id}", sub.Id);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("web_push: RecipientUserId is required");
                    }
                    break;
                default:
                    _logger.LogWarning("Unknown notification type '{Type}' — skipping", type);
                    break;
            }
        }
    }
}
