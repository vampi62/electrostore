using System.Reflection;
using Confluent.Kafka;
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
using Metadata = Grpc.Core.Metadata;

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

    private static ConsumeResult<string, string> CreateConsumeResult(string? value, bool isPartitionEOF = false, long offset = 1)
    {
        return new ConsumeResult<string, string>
        {
            Message = value is null ? null : new Message<string, string> { Value = value },
            IsPartitionEOF = isPartitionEOF,
            Offset = offset
        };
    }

    private static Task<ConsumeResult<string, string>?> ConsumeMessageAsync(KafkaNotifConsumer consumer, IConsumer<string, string> kafkaConsumer, CancellationToken ct = default)
    {
        var method = typeof(KafkaNotifConsumer).GetMethod("ConsumeMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ConsumeMessageAsync method not found");
        return (Task<ConsumeResult<string, string>?>)method.Invoke(consumer, new object[] { kafkaConsumer, ct })!;
    }

    private static Task ProcessMessageAsync(KafkaNotifConsumer consumer, IConsumer<string, string> kafkaConsumer, ConsumeResult<string, string> result, CancellationToken ct = default)
    {
        var method = typeof(KafkaNotifConsumer).GetMethod("ProcessMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ProcessMessageAsync method not found");
        return (Task)method.Invoke(consumer, new object[] { kafkaConsumer, result, ct })!;
    }

    private static NotificationMessage? DeserializeMessage(KafkaNotifConsumer consumer, ConsumeResult<string, string> result)
    {
        var method = typeof(KafkaNotifConsumer).GetMethod("DeserializeMessage", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("DeserializeMessage method not found");
        return (NotificationMessage?)method.Invoke(consumer, new object[] { result });
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

    // ---- DeserializeMessage ----

    [Fact]
    public void DeserializeMessage_ShouldReturnMessage_WhenJsonIsValid()
    {
        // Arrange
        var consumer = CreateConsumer();
        var result = CreateConsumeResult("""{"types":["email"],"recipientEmail":"user@example.com"}""");

        // Act
        var msg = DeserializeMessage(consumer, result);

        // Assert
        Assert.NotNull(msg);
        Assert.Equal("user@example.com", msg!.RecipientEmail);
    }

    [Fact]
    public void DeserializeMessage_ShouldReturnNull_WhenJsonIsInvalid()
    {
        // Arrange
        var consumer = CreateConsumer();
        var result = CreateConsumeResult("{not-json");

        // Act
        var msg = DeserializeMessage(consumer, result);

        // Assert
        Assert.Null(msg);
    }

    // ---- ConsumeMessageAsync ----

    [Fact]
    public async Task ConsumeMessageAsync_ShouldReturnResult_WhenConsumeSucceeds()
    {
        // Arrange
        var consumer = CreateConsumer();
        var expected = CreateConsumeResult("""{"types":["email"],"recipientEmail":"user@example.com"}""");
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        kafkaConsumer.Setup(c => c.Consume(It.IsAny<CancellationToken>())).Returns(expected);

        // Act
        var result = await ConsumeMessageAsync(consumer, kafkaConsumer.Object);

        // Assert
        Assert.Same(expected, result);
    }

    [Fact]
    public async Task ConsumeMessageAsync_ShouldReturnNull_WhenOperationIsCancelled()
    {
        // Arrange
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        kafkaConsumer.Setup(c => c.Consume(It.IsAny<CancellationToken>())).Throws<OperationCanceledException>();

        // Act
        var result = await ConsumeMessageAsync(consumer, kafkaConsumer.Object);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ConsumeMessageAsync_ShouldReturnNull_WhenConsumeExceptionIsThrown()
    {
        // Arrange
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        kafkaConsumer
            .Setup(c => c.Consume(It.IsAny<CancellationToken>()))
            .Throws(new ConsumeException(new ConsumeResult<byte[], byte[]>(), new Error(ErrorCode.UnknownTopicOrPart)));

        // Act
        var result = await ConsumeMessageAsync(consumer, kafkaConsumer.Object);

        // Assert
        Assert.Null(result);
    }

    // ---- ProcessMessageAsync ----

    [Fact]
    public async Task ProcessMessageAsync_ShouldDoNothing_WhenResultIsPartitionEOF()
    {
        // Arrange
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        var result = CreateConsumeResult(null, isPartitionEOF: true);

        // Act
        await ProcessMessageAsync(consumer, kafkaConsumer.Object, result);

        // Assert
        kafkaConsumer.Verify(c => c.Commit(It.IsAny<ConsumeResult<string, string>>()), Times.Never);
        _emailService.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldDoNothing_WhenMessageValueIsNull()
    {
        // Arrange
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        var result = CreateConsumeResult(null);

        // Act
        await ProcessMessageAsync(consumer, kafkaConsumer.Object, result);

        // Assert
        kafkaConsumer.Verify(c => c.Commit(It.IsAny<ConsumeResult<string, string>>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldCommitWithoutDispatching_WhenJsonIsInvalid()
    {
        // Arrange
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        var result = CreateConsumeResult("{not-json");

        // Act
        await ProcessMessageAsync(consumer, kafkaConsumer.Object, result);

        // Assert
        kafkaConsumer.Verify(c => c.Commit(result), Times.Once);
        _emailService.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldDispatchAndCommit_WhenMessageIsValid()
    {
        // Arrange
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        var result = CreateConsumeResult("""{"types":["email"],"recipientEmail":"user@example.com","subject":"S","body":"B"}""");

        // Act
        await ProcessMessageAsync(consumer, kafkaConsumer.Object, result);

        // Assert
        _emailService.Verify(e => e.SendAsync("user@example.com", "S", "B"), Times.Once);
        kafkaConsumer.Verify(c => c.Commit(result), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldNotCommitOrThrow_WhenDispatchThrows()
    {
        // Arrange
        var consumer = CreateConsumer();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        var result = CreateConsumeResult("""{"types":["email"],"recipientEmail":"user@example.com","subject":"S","body":"B"}""");
        _emailService
            .Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        // Act
        var exception = await Record.ExceptionAsync(() => ProcessMessageAsync(consumer, kafkaConsumer.Object, result));

        // Assert
        Assert.Null(exception);
        kafkaConsumer.Verify(c => c.Commit(It.IsAny<ConsumeResult<string, string>>()), Times.Never);
    }
}
