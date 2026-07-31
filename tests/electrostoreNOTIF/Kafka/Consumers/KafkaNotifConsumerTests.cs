using System.Reflection;
using ElectrostoreNOTIF.Grpc;
using ElectrostoreNOTIF.Kafka.Consumers;
using ElectrostoreNOTIF.Kafka.Messages;
using ElectrostoreNOTIF.Services.EmailSenderService;
using ElectrostoreNOTIF.Services.NotificationTemplateService;
using ElectrostoreNOTIF.Services.WebPushService;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElectrostoreNOTIF.Tests.Kafka.Consumers;

public class KafkaNotifConsumerTests
{
    private readonly Mock<IEmailSenderService> _emailService = new();
    private readonly Mock<INotificationTemplateService> _templateService = new();
    private readonly Mock<IWebPushService> _webPushService = new();
    private readonly Mock<UsersGrpc.UsersGrpcClient> _userResolver = new();
    private readonly Mock<ILogger<KafkaNotifConsumer>> _logger = new();

    private KafkaNotifConsumer CreateConsumer()
    {
        var configuration = new ConfigurationBuilder().Build();
        return new KafkaNotifConsumer(
            configuration,
            _emailService.Object,
            _templateService.Object,
            _webPushService.Object,
            _userResolver.Object,
            _logger.Object);
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

    private static AsyncUnaryCall<TResponse> CreateFailingAsyncUnaryCall<TResponse>(Exception exception)
    {
        return new AsyncUnaryCall<TResponse>(
            Task.FromException<TResponse>(exception),
            Task.FromException<Metadata>(exception),
            () => Status.DefaultCancelled,
            () => new Metadata(),
            () => { });
    }

    private static Task DispatchAsync(KafkaNotifConsumer consumer, NotificationMessage message, CancellationToken ct = default)
    {
        var method = typeof(KafkaNotifConsumer).GetMethod("DispatchAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("DispatchAsync method not found");
        return (Task)method.Invoke(consumer, new object[] { message, ct })!;
    }

    [Fact]
    public async Task DispatchAsync_ShouldSendEmail_WhenTypeIsEmailAndRecipientEmailProvided()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = new NotificationMessage
        {
            Types = ["email"],
            RecipientEmail = "user@example.com",
            Subject = "Hello",
            Body = "World"
        };

        // Act
        await DispatchAsync(consumer, message);

        // Assert
        _emailService.Verify(e => e.SendAsync("user@example.com", "Hello", "World"), Times.Once);
        _userResolver.Verify(u => u.GetUserInfoAsync(It.IsAny<GetUserInfoRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_ShouldUseRenderedTemplate_WhenTemplateIdIsProvided()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = new NotificationMessage
        {
            Types = ["email"],
            RecipientEmail = "user@example.com",
            TemplateId = "account-created",
            Language = "en"
        };
        _templateService
            .Setup(t => t.RenderTemplate("account-created", message.TemplateValues, "en"))
            .Returns(new NotificationTemplateRender
            {
                Subject = "Rendered subject",
                Body = "Rendered body"
            });

        // Act
        await DispatchAsync(consumer, message);

        // Assert
        _emailService.Verify(e => e.SendAsync("user@example.com", "Rendered subject", "Rendered body"), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_ShouldFallBackToRawFields_WhenTemplateRenderingFails()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = new NotificationMessage
        {
            Types = ["email"],
            RecipientEmail = "user@example.com",
            TemplateId = "unknown-template",
            Subject = "Fallback subject",
            Body = "Fallback body"
        };
        _templateService
            .Setup(t => t.RenderTemplate("unknown-template", message.TemplateValues, null))
            .Returns((NotificationTemplateRender?)null);

        // Act
        await DispatchAsync(consumer, message);

        // Assert
        _emailService.Verify(e => e.SendAsync("user@example.com", "Fallback subject", "Fallback body"), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_ShouldResolveEmailFromApi_WhenRecipientEmailIsMissing()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = new NotificationMessage
        {
            Types = ["email"],
            RecipientUserId = 42,
            Subject = "Hello",
            Body = "World"
        };
        _userResolver
            .Setup(u => u.GetUserInfoAsync(It.Is<GetUserInfoRequest>(r => r.UserId == 42), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(new GetUserInfoReply { Found = true, Email = "resolved@example.com" }));

        // Act
        await DispatchAsync(consumer, message);

        // Assert
        _emailService.Verify(e => e.SendAsync("resolved@example.com", "Hello", "World"), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_ShouldNotSendEmail_WhenUserIsNotFoundInApi()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = new NotificationMessage
        {
            Types = ["email"],
            RecipientUserId = 42
        };
        _userResolver
            .Setup(u => u.GetUserInfoAsync(It.IsAny<GetUserInfoRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(new GetUserInfoReply { Found = false }));

        // Act
        await DispatchAsync(consumer, message);

        // Assert
        _emailService.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_ShouldNotSendEmail_WhenUserResolutionThrowsRpcException()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = new NotificationMessage
        {
            Types = ["email"],
            RecipientUserId = 42
        };
        _userResolver
            .Setup(u => u.GetUserInfoAsync(It.IsAny<GetUserInfoRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateFailingAsyncUnaryCall<GetUserInfoReply>(new RpcException(new Status(StatusCode.Unavailable, "down"))));

        // Act
        await DispatchAsync(consumer, message);

        // Assert
        _emailService.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_ShouldNotSendEmail_WhenNoEmailAddressCanBeResolved()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = new NotificationMessage
        {
            Types = ["email"]
        };

        // Act
        await DispatchAsync(consumer, message);

        // Assert
        _emailService.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_ShouldSendWebPush_ForEachSubscription()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = new NotificationMessage
        {
            Types = ["webpush"],
            RecipientEmail = "unused@example.com",
            RecipientUserId = 7,
            Title = "Title",
            Body = "Body"
        };
        var reply = new GetUserPushSubscriptionsReply();
        reply.Subscriptions.Add(new PushSubscriptionItem { Id = 1, Endpoint = "https://push/1", P256Dh = "key1", Auth = "auth1" });
        reply.Subscriptions.Add(new PushSubscriptionItem { Id = 2, Endpoint = "https://push/2", P256Dh = "key2", Auth = "auth2" });
        _userResolver
            .Setup(u => u.GetUserPushSubscriptionsAsync(It.Is<GetUserPushSubscriptionsRequest>(r => r.UserId == 7), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(reply));

        // Act
        await DispatchAsync(consumer, message);

        // Assert
        _webPushService.Verify(w => w.SendAsync("https://push/1", "key1", "auth1", "Title", "Body", null), Times.Once);
        _webPushService.Verify(w => w.SendAsync("https://push/2", "key2", "auth2", "Title", "Body", null), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_ShouldNotSendWebPush_WhenSubscriptionFetchFails()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = new NotificationMessage
        {
            Types = ["webpush"],
            RecipientEmail = "unused@example.com",
            RecipientUserId = 7
        };
        _userResolver
            .Setup(u => u.GetUserPushSubscriptionsAsync(It.IsAny<GetUserPushSubscriptionsRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateFailingAsyncUnaryCall<GetUserPushSubscriptionsReply>(new RpcException(new Status(StatusCode.Unavailable, "down"))));

        // Act
        var exception = await Record.ExceptionAsync(() => DispatchAsync(consumer, message));

        // Assert
        Assert.Null(exception);
        _webPushService.Verify(w => w.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>?>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_ShouldNotSendWebPush_WhenRecipientUserIdIsMissing()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = new NotificationMessage
        {
            Types = ["webpush"]
        };

        // Act
        await DispatchAsync(consumer, message);

        // Assert
        _webPushService.Verify(w => w.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>?>()), Times.Never);
        _userResolver.Verify(u => u.GetUserPushSubscriptionsAsync(It.IsAny<GetUserPushSubscriptionsRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_ShouldNotThrow_WhenNotificationTypeIsUnknown()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = new NotificationMessage
        {
            Types = ["carrier-pigeon"]
        };

        // Act
        var exception = await Record.ExceptionAsync(() => DispatchAsync(consumer, message));

        // Assert
        Assert.Null(exception);
        _emailService.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _webPushService.Verify(w => w.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>?>()), Times.Never);
    }
}
