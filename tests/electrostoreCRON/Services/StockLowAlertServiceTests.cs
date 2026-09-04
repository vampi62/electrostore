using System.Text.Json;
using ElectrostoreCRON.Grpc;
using ElectrostoreCRON.Kafka.Producer;
using ElectrostoreCRON.Services.StockLowAlertService;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElectrostoreCRON.Tests.Services;

public class StockLowAlertServiceTests
{
    private readonly Mock<ItemsGrpc.ItemsGrpcClient> _apiClient = new();
    private readonly Mock<IKafkaProducerService> _kafka = new();
    private readonly Mock<ILogger<StockLowAlertService>> _logger = new();

    private StockLowAlertService CreateService(Dictionary<string, string?>? extraConfig = null)
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
        return new StockLowAlertService(_apiClient.Object, _kafka.Object, configuration, _logger.Object);
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

    private void SetupReport(GetLowStockItemsReply reply)
    {
        _apiClient
            .Setup(c => c.GetLowStockItemsAsync(It.IsAny<GetLowStockItemsRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(reply));
    }

    private static GetLowStockItemsReply BuildReply(int itemCount = 1, int recipientCount = 1)
    {
        var reply = new GetLowStockItemsReply();
        for (var i = 0; i < itemCount; i++)
        {
            reply.Items.Add(new LowStockItemItem
            {
                IdItem = 42 + i,
                ReferenceNameItem = $"R10K-{i}",
                FriendlyNameItem = "Resistor 10k",
                QuantityItem = 2,
                ThresholdMinItem = 10
            });
        }
        for (var i = 0; i < recipientCount; i++)
        {
            reply.Recipients.Add(new LowStockRecipientItem
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
    public async Task SendAlertAsync_ShouldPublishOneNotificationPerRecipient()
    {
        // Arrange
        var service = CreateService();
        SetupReport(BuildReply(itemCount: 2, recipientCount: 3));

        // Act
        await service.SendAlertAsync(null);

        // Assert
        _kafka.Verify(k => k.PublishAsync(
            "notification-requests", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task SendAlertAsync_ShouldSendTemplateValuesWithItemArray()
    {
        // Arrange
        var service = CreateService();
        SetupReport(BuildReply(itemCount: 1, recipientCount: 1));
        string? published = null;
        _kafka
            .Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, _, value, _) => published = value)
            .Returns(Task.CompletedTask);

        // Act
        await service.SendAlertAsync(null);

        // Assert
        Assert.NotNull(published);
        var message = CapturePublishedMessage(published!);
        Assert.Equal("stock-low-alert", message.GetProperty("TemplateId").GetString());
        Assert.Equal("admin0@example.com", message.GetProperty("RecipientEmail").GetString());
        Assert.Equal("fr", message.GetProperty("Language").GetString());
        var values = message.GetProperty("TemplateValues");
        Assert.Equal(1, values.GetProperty("itemCount").GetInt32());
        var items = values.GetProperty("items");
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
        var row = items[0];
        Assert.Equal("Resistor 10k", row.GetProperty("item").GetString());
        Assert.Equal("R10K-0", row.GetProperty("reference").GetString());
        Assert.Equal(2, row.GetProperty("quantity").GetInt32());
        Assert.Equal(10, row.GetProperty("threshold").GetInt32());
    }

    [Fact]
    public async Task SendAlertAsync_ShouldUseLanguageFromParams_OverridingAppLanguage()
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
        await service.SendAlertAsync("{\"language\":\"en\"}");

        // Assert
        var message = CapturePublishedMessage(published!);
        Assert.Equal("en", message.GetProperty("Language").GetString());
    }

    [Fact]
    public async Task SendAlertAsync_ShouldFallBackToDefaults_WhenParamsAreInvalid()
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
        await service.SendAlertAsync("not-json");

        // Assert
        var message = CapturePublishedMessage(published!);
        Assert.Equal("fr", message.GetProperty("Language").GetString());
        Assert.Equal("email", message.GetProperty("Types")[0].GetString());
    }

    [Fact]
    public async Task SendAlertAsync_ShouldNotPublish_WhenThereIsNoLowStockItem()
    {
        // Arrange
        var service = CreateService();
        SetupReport(BuildReply(itemCount: 0));

        // Act
        await service.SendAlertAsync(null);

        // Assert
        _kafka.Verify(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAlertAsync_ShouldNotPublish_WhenThereIsNoAdministrator()
    {
        // Arrange
        var service = CreateService();
        SetupReport(BuildReply(recipientCount: 0));

        // Act
        await service.SendAlertAsync(null);

        // Assert
        _kafka.Verify(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAlertAsync_ShouldNotThrow_WhenApiCallFails()
    {
        // Arrange
        var service = CreateService();
        _apiClient
            .Setup(c => c.GetLowStockItemsAsync(It.IsAny<GetLowStockItemsRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateFailingAsyncUnaryCall<GetLowStockItemsReply>(new RpcException(new Status(StatusCode.Unavailable, "down"))));

        // Act
        var exception = await Record.ExceptionAsync(() => service.SendAlertAsync(null));

        // Assert
        Assert.Null(exception);
        _kafka.Verify(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAlertAsync_ShouldKeepNotifyingOtherRecipients_WhenOnePublishFails()
    {
        // Arrange
        var service = CreateService();
        SetupReport(BuildReply(recipientCount: 2));
        _kafka
            .SetupSequence(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("kafka down"))
            .Returns(Task.CompletedTask);

        // Act
        var exception = await Record.ExceptionAsync(() => service.SendAlertAsync(null));

        // Assert
        Assert.Null(exception);
        _kafka.Verify(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
