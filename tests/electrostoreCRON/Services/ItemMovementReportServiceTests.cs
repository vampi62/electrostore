using System.Text.Json;
using ElectrostoreCRON.Grpc;
using ElectrostoreCRON.Kafka.Producer;
using ElectrostoreCRON.Services.ItemMovementReportService;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElectrostoreCRON.Tests.Services;

public class ItemMovementReportServiceTests
{
    private readonly Mock<ItemsHistoryGrpc.ItemsHistoryGrpcClient> _apiClient = new();
    private readonly Mock<IKafkaProducerService> _kafka = new();
    private readonly Mock<ILogger<ItemMovementReportService>> _logger = new();

    private ItemMovementReportService CreateService(Dictionary<string, string?>? extraConfig = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["AppLanguage"] = "fr"
        };
        if (extraConfig is not null)
        {
            foreach (var kvp in extraConfig) values[kvp.Key] = kvp.Value;
        }
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return new ItemMovementReportService(_apiClient.Object, _kafka.Object, configuration, _logger.Object);
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

    private void SetupReport(GetItemsMovementReportReply reply, Action<GetItemsMovementReportRequest>? capture = null)
    {
        _apiClient
            .Setup(c => c.GetItemsMovementReportAsync(It.IsAny<GetItemsMovementReportRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Callback<GetItemsMovementReportRequest, Metadata, DateTime?, CancellationToken>((req, _, _, _) => capture?.Invoke(req))
            .Returns(CreateAsyncUnaryCall(reply));
    }

    private static GetItemsMovementReportReply BuildReply(int movementCount = 1, int recipientCount = 1)
    {
        var reply = new GetItemsMovementReportReply
        {
            FromDate = "2026-08-28T10:00:00.0000000Z",
            ToDate = "2026-09-04T10:00:00.0000000Z"
        };
        for (var i = 0; i < movementCount; i++)
        {
            reply.Movements.Add(new ItemMovementItem
            {
                IdItemHistory = i + 1,
                IdItem = 42,
                ItemName = "Resistor 10k",
                Type = "StockAdded",
                QuantityChange = 5,
                OldQuantity = 10,
                NewQuantity = 15,
                IdUser = 3,
                UserName = "John Doe",
                Notes = "restock",
                CreatedAt = "2026-09-01T08:30:00.0000000Z"
            });
        }
        for (var i = 0; i < recipientCount; i++)
        {
            reply.Recipients.Add(new ReportRecipientItem
            {
                IdUser = i + 1,
                Email = $"admin{i}@example.com",
                Firstname = "Ada",
                Name = "Lovelace"
            });
        }
        return reply;
    }

    private static JsonElement CapturePublishedMessage(string payload) =>
        JsonDocument.Parse(payload).RootElement;

    [Fact]
    public async Task SendReportAsync_ShouldPublishOneNotificationPerRecipient()
    {
        // Arrange
        var service = CreateService();
        SetupReport(BuildReply(movementCount: 2, recipientCount: 3));

        // Act
        await service.SendReportAsync(null);

        // Assert
        _kafka.Verify(k => k.PublishAsync(
            "notification-requests", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task SendReportAsync_ShouldSendTemplateValuesWithMovementArray()
    {
        // Arrange
        var service = CreateService();
        SetupReport(BuildReply(movementCount: 1, recipientCount: 1));
        string? published = null;
        _kafka
            .Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, _, value, _) => published = value)
            .Returns(Task.CompletedTask);

        // Act
        await service.SendReportAsync(null);

        // Assert
        Assert.NotNull(published);
        var message = CapturePublishedMessage(published!);
        Assert.Equal("weekly-item-movement-report", message.GetProperty("TemplateId").GetString());
        Assert.Equal("admin0@example.com", message.GetProperty("RecipientEmail").GetString());
        var values = message.GetProperty("TemplateValues");
        Assert.Equal(1, values.GetProperty("movementCount").GetInt32());
        Assert.Equal("2026-08-28", values.GetProperty("fromDate").GetString());
        Assert.Equal("2026-09-04", values.GetProperty("toDate").GetString());
        var movements = values.GetProperty("movements");
        Assert.Equal(JsonValueKind.Array, movements.ValueKind);
        var row = movements[0];
        Assert.Equal("Resistor 10k", row.GetProperty("item").GetString());
        Assert.Equal("+5", row.GetProperty("quantityChange").GetString());
        Assert.Equal("Stock ajouté", row.GetProperty("type").GetString());
        Assert.Equal("2026-09-01 08:30", row.GetProperty("date").GetString());
    }

    [Fact]
    public async Task SendReportAsync_ShouldUseEnglishLabels_WhenLanguageIsOverriddenByParams()
    {
        // Arrange
        var service = CreateService();
        SetupReport(BuildReply());
        string? published = null;
        _kafka
            .Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, _, value, _) => published = value)
            .Returns(Task.CompletedTask);

        // Act
        await service.SendReportAsync("{\"language\":\"en\"}");

        // Assert
        var message = CapturePublishedMessage(published!);
        Assert.Equal("en", message.GetProperty("Language").GetString());
        Assert.Equal("Stock added", message.GetProperty("TemplateValues").GetProperty("movements")[0].GetProperty("type").GetString());
    }

    [Fact]
    public async Task SendReportAsync_ShouldRequestTheConfiguredPeriod()
    {
        // Arrange
        var service = CreateService();
        GetItemsMovementReportRequest? request = null;
        SetupReport(BuildReply(), req => request = req);

        // Act
        await service.SendReportAsync("{\"days\":30}");

        // Assert
        Assert.NotNull(request);
        var from = DateTime.Parse(request!.FromDate, null, System.Globalization.DateTimeStyles.RoundtripKind);
        var to = DateTime.Parse(request.ToDate, null, System.Globalization.DateTimeStyles.RoundtripKind);
        Assert.Equal(30, (to - from).Days);
    }

    [Fact]
    public async Task SendReportAsync_ShouldFallBackToSevenDays_WhenParamsAreInvalid()
    {
        // Arrange
        var service = CreateService();
        GetItemsMovementReportRequest? request = null;
        SetupReport(BuildReply(), req => request = req);

        // Act
        await service.SendReportAsync("not-json");

        // Assert
        var from = DateTime.Parse(request!.FromDate, null, System.Globalization.DateTimeStyles.RoundtripKind);
        var to = DateTime.Parse(request.ToDate, null, System.Globalization.DateTimeStyles.RoundtripKind);
        Assert.Equal(7, (to - from).Days);
    }

    [Fact]
    public async Task SendReportAsync_ShouldNotPublish_WhenThereIsNoMovement()
    {
        // Arrange
        var service = CreateService();
        SetupReport(BuildReply(movementCount: 0));

        // Act
        await service.SendReportAsync(null);

        // Assert
        _kafka.Verify(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendReportAsync_ShouldPublish_WhenThereIsNoMovementButSendWhenEmptyIsSet()
    {
        // Arrange
        var service = CreateService();
        SetupReport(BuildReply(movementCount: 0));

        // Act
        await service.SendReportAsync("{\"send_when_empty\":true}");

        // Assert
        _kafka.Verify(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendReportAsync_ShouldNotPublish_WhenThereIsNoAdministrator()
    {
        // Arrange
        var service = CreateService();
        SetupReport(BuildReply(recipientCount: 0));

        // Act
        await service.SendReportAsync(null);

        // Assert
        _kafka.Verify(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendReportAsync_ShouldNotThrow_WhenApiCallFails()
    {
        // Arrange
        var service = CreateService();
        _apiClient
            .Setup(c => c.GetItemsMovementReportAsync(It.IsAny<GetItemsMovementReportRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateFailingAsyncUnaryCall<GetItemsMovementReportReply>(new RpcException(new Status(StatusCode.Unavailable, "down"))));

        // Act
        var exception = await Record.ExceptionAsync(() => service.SendReportAsync(null));

        // Assert
        Assert.Null(exception);
        _kafka.Verify(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendReportAsync_ShouldKeepNotifyingOtherRecipients_WhenOnePublishFails()
    {
        // Arrange
        var service = CreateService();
        SetupReport(BuildReply(recipientCount: 2));
        _kafka
            .SetupSequence(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("kafka down"))
            .Returns(Task.CompletedTask);

        // Act
        var exception = await Record.ExceptionAsync(() => service.SendReportAsync(null));

        // Assert
        Assert.Null(exception);
        _kafka.Verify(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
