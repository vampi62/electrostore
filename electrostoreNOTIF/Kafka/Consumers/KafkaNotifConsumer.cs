using System.Text.Json;
using Confluent.Kafka;
using ElectrostoreNOTIF.Kafka.Messages;
using ElectrostoreNOTIF.Services.EmailSenderService;
using ElectrostoreNOTIF.Services.NotificationTemplateService;
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
    private readonly INotificationTemplateService _templateService;
    private readonly string topic = "notification-requests";
    private readonly IWebPushService _webPush;
    private readonly UsersGrpc.UsersGrpcClient _userResolver;
    private readonly ILogger<KafkaNotifConsumer> _logger;

    public KafkaNotifConsumer(
        IConfiguration configuration,
        IEmailSenderService email,
        INotificationTemplateService templateService,
        IWebPushService webPush,
        UsersGrpc.UsersGrpcClient userResolver,
        ILogger<KafkaNotifConsumer> logger)
    {
        _configuration = configuration;
        _email = email;
        _templateService = templateService;
        _webPush = webPush;
        _userResolver = userResolver;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrapServers = _configuration.GetSection("Kafka:BootstrapServers").Value ?? "kafka:9092";
        var groupId = _configuration.GetSection("Kafka:ConsumerGroupId").Value ?? "notif-service";
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
        consumer.Subscribe(topic);
        _logger.LogInformation(
            "KafkaNotifConsumer started (group={Group}, servers={Servers})",
            groupId, bootstrapServers);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = await ConsumeMessageAsync(consumer, stoppingToken);
                if (result is null)
                {
                    continue;
                }
                await ProcessMessageAsync(consumer, result, stoppingToken);
            }
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("KafkaNotifConsumer stopped");
        }
    }

    private async Task<ConsumeResult<string, string>?> ConsumeMessageAsync(
        IConsumer<string, string> consumer, 
        CancellationToken ct)
    {
        try
        {
            return await Task.Run(() => consumer.Consume(ct), ct);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (ConsumeException ex)
        {
            _logger.LogError(ex, "Kafka error: {Reason}", ex.Error.Reason);
            return null;
        }
    }

    private async Task ProcessMessageAsync(
        IConsumer<string, string> consumer,
        ConsumeResult<string, string> result,
        CancellationToken ct)
    {
        if (result.IsPartitionEOF || result.Message?.Value is null)
        {
            return;
        }
        var msg = DeserializeMessage(result);
        if (msg is null)
        {
            consumer.Commit(result);
            return;
        }
        var dispatched = await DispatchAsync(msg, ct);
        if (dispatched)
        {
            consumer.Commit(result);
        } else
        {
            _logger.LogWarning("Message for offset {Offset} was not dispatched.", result.Offset);
        }
    }

    private NotificationMessage? DeserializeMessage(ConsumeResult<string, string> result)
    {
        try
        {
            return JsonSerializer.Deserialize<NotificationMessage>(result.Message.Value, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid Kafka message (JSON) - offset {Offset}", result.Offset);
            return null;
        }
    }

    private async Task<bool> DispatchAsync(NotificationMessage msg, CancellationToken ct)
    {
        var rendered = RenderTemplateIfNeededAsync(msg);
        var emailAddress = await ResolveEmailAddressAsync(msg, ct);
        
        var notificationContent = new NotificationContent
        {
            EmailAddress = emailAddress,
            EmailSubject = rendered?.Subject ?? msg.Subject,
            EmailBody = rendered?.Body ?? msg.Body,
            PushTitle = rendered?.Title ?? msg.Title,
            PushBody = rendered?.Body ?? msg.Body,
            PushData = rendered?.Data ?? msg.PushData
        };

        foreach (var type in msg.Types)
        {
            await SendNotificationByTypeAsync(type, msg, notificationContent, ct);
        }
        
        return true;
    }

    private NotificationTemplateRender? RenderTemplateIfNeededAsync(NotificationMessage msg)
    {
        if (string.IsNullOrWhiteSpace(msg.TemplateId))
        {
            return null;
        }
        var rendered = _templateService.RenderTemplate(msg.TemplateId, msg.TemplateValues, msg.Language);
        if (rendered is null)
        {
            _logger.LogWarning("Template '{TemplateId}' could not be rendered.", msg.TemplateId);
        }
        return rendered;
    }

    private async Task<string?> ResolveEmailAddressAsync(NotificationMessage msg, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(msg.RecipientEmail))
        {
            return msg.RecipientEmail;
        }
        if (!msg.RecipientUserId.HasValue)
        {
            return null;
        }
        try
        {
            var reply = await _userResolver.GetUserInfoAsync(
                new GetUserInfoRequest { UserId = msg.RecipientUserId.Value },
                cancellationToken: ct);
            if (!reply.Found)
            {
                _logger.LogWarning("User {Id} not found in API", msg.RecipientUserId.Value);
                return null;
            }
            return reply.Email;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Error while calling API to get user info (userId={UserId})", msg.RecipientUserId.Value);
            return null;
        }
    }

    private async Task SendNotificationByTypeAsync(
        string type, 
        NotificationMessage msg, 
        NotificationContent content, 
        CancellationToken ct)
    {
        switch (type.ToLowerInvariant())
        {
            case "email":
                await SendEmailNotificationAsync(msg, content);
                break;
            case "webpush":
                await SendWebPushNotificationAsync(msg, content, ct);
                break;
            default:
                _logger.LogWarning("Unknown notification type '{Type}' - skipping", type);
                break;
        }
    }

    private async Task SendEmailNotificationAsync(NotificationMessage msg, NotificationContent content)
    {
        if (!string.IsNullOrEmpty(content.EmailAddress))
        {
            await _email.SendAsync(
                content.EmailAddress, 
                content.EmailSubject ?? string.Empty, 
                content.EmailBody ?? string.Empty);
        }
        else
        {
            _logger.LogWarning(
                "Cannot send email notification - no email address for user ID {UserId}",
                msg.RecipientUserId);
        }
    }

    private async Task SendWebPushNotificationAsync(
        NotificationMessage msg, 
        NotificationContent content, 
        CancellationToken ct)
    {
        if (!msg.RecipientUserId.HasValue)
        {
            _logger.LogWarning("webPush: RecipientUserId is required");
            return;
        }
        GetUserPushSubscriptionsReply pushSubs;
        try
        {
            pushSubs = await _userResolver.GetUserPushSubscriptionsAsync(
                new GetUserPushSubscriptionsRequest { UserId = msg.RecipientUserId.Value },
                cancellationToken: ct);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Error fetching push subscriptions for user {UserId}", msg.RecipientUserId.Value);
            return;
        }
        foreach (var sub in pushSubs.Subscriptions)
        {
            try
            {
                await _webPush.SendAsync(
                    sub.Endpoint, 
                    sub.P256Dh, 
                    sub.Auth, 
                    content.PushTitle ?? string.Empty, 
                    content.PushBody ?? string.Empty, 
                    content.PushData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send web push to subscription {Id}", sub.Id);
            }
        }
    }

    private class NotificationContent
    {
        public string? EmailAddress { get; set; }
        public string? EmailSubject { get; set; }
        public string? EmailBody { get; set; }
        public string? PushTitle { get; set; }
        public string? PushBody { get; set; }
        public Dictionary<string, string>? PushData { get; set; }
    }
}
