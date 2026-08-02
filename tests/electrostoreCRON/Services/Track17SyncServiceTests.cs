using System.Net;
using System.Reflection;
using System.Text.Json;
using Confluent.Kafka;
using ElectrostoreCRON.Kafka.Messages;
using ElectrostoreCRON.Kafka.Producer;
using ElectrostoreCRON.Services.Track17SyncService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace ElectrostoreCRON.Tests.Services;

public class Track17SyncServiceTests
{
    private readonly Mock<IKafkaProducerService> _kafka = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly Mock<HttpMessageHandler> _httpMessageHandler = new();
    private readonly Mock<ILogger<Track17SyncService>> _logger = new();

    public Track17SyncServiceTests()
    {
        _httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(_httpMessageHandler.Object));
    }

    private Track17SyncService CreateService(Dictionary<string, string?>? extraConfig = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["Track17:BaseUrl"] = "https://track17.test"
        };
        if (extraConfig is not null)
        {
            foreach (var kvp in extraConfig) values[kvp.Key] = kvp.Value;
        }
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return new Track17SyncService(_kafka.Object, _httpClientFactory.Object, configuration, _logger.Object);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string? content, Action<HttpRequestMessage>? capture = null)
    {
        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capture?.Invoke(req))
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = content is null ? null : new StringContent(content)
            });
    }

    private void SetupHttpThrows(Exception exception)
    {
        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(exception);
    }

    private static Task<TrackingResultMessage[]> Call17TrackAsync(
        Track17SyncService service, string action, string endpoint, string apiKey, List<TrackingActionMessage> messages)
    {
        var method = typeof(Track17SyncService).GetMethod("Call17TrackAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Call17TrackAsync method not found");
        return (Task<TrackingResultMessage[]>)method.Invoke(service, new object[] { action, endpoint, apiKey, messages, CancellationToken.None })!;
    }

    private static object[] BuildRequestItems(string action, List<TrackingActionMessage> messages)
    {
        var method = typeof(Track17SyncService).GetMethod("BuildRequestItems", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("BuildRequestItems method not found");
        return (object[])method.Invoke(null, new object[] { action, messages })!;
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

    private static Task<ConsumeResult<string, string>?> ConsumeMessageAsync(Track17SyncService service, IConsumer<string, string> kafkaConsumer, CancellationToken ct = default)
    {
        var method = typeof(Track17SyncService).GetMethod("ConsumeMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ConsumeMessageAsync method not found");
        return (Task<ConsumeResult<string, string>?>)method.Invoke(service, new object[] { kafkaConsumer, ct })!;
    }

    private static TrackingActionMessage? DeserializeMessage(Track17SyncService service, ConsumeResult<string, string> result)
    {
        var method = typeof(Track17SyncService).GetMethod("DeserializeMessage", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("DeserializeMessage method not found");
        return (TrackingActionMessage?)method.Invoke(service, new object[] { result });
    }

    // ---- SyncAllAsync ----

    [Fact]
    public async Task SyncAllAsync_ShouldSkip_WhenApiKeyIsMissing()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.SyncAllAsync();

        // Assert
        _httpClientFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
        _kafka.Verify(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- DeserializeMessage ----

    [Fact]
    public void DeserializeMessage_ShouldReturnMessage_WhenJsonIsValid()
    {
        // Arrange
        var service = CreateService();
        var result = CreateConsumeResult("""{"tracking_number":"TN1","carrier":100}""");

        // Act
        var msg = DeserializeMessage(service, result);

        // Assert
        Assert.NotNull(msg);
        Assert.Equal("TN1", msg!.tracking_number);
    }

    [Fact]
    public void DeserializeMessage_ShouldReturnNull_WhenJsonIsInvalid()
    {
        // Arrange
        var service = CreateService();
        var result = CreateConsumeResult("{not-json");

        // Act
        var msg = DeserializeMessage(service, result);

        // Assert
        Assert.Null(msg);
    }

    // ---- ConsumeMessageAsync ----

    [Fact]
    public async Task ConsumeMessageAsync_ShouldReturnResult_WhenConsumeSucceeds()
    {
        // Arrange
        var service = CreateService();
        var expected = CreateConsumeResult("""{"tracking_number":"TN1","carrier":100}""");
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        kafkaConsumer.Setup(c => c.Consume(It.IsAny<TimeSpan>())).Returns(expected);

        // Act
        var result = await ConsumeMessageAsync(service, kafkaConsumer.Object);

        // Assert
        Assert.Same(expected, result);
    }

    [Fact]
    public async Task ConsumeMessageAsync_ShouldReturnNull_WhenOperationIsCancelled()
    {
        // Arrange
        var service = CreateService();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        kafkaConsumer.Setup(c => c.Consume(It.IsAny<TimeSpan>())).Throws<OperationCanceledException>();

        // Act
        var result = await ConsumeMessageAsync(service, kafkaConsumer.Object);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ConsumeMessageAsync_ShouldReturnNull_WhenConsumeExceptionIsThrown()
    {
        // Arrange
        var service = CreateService();
        var kafkaConsumer = new Mock<IConsumer<string, string>>();
        kafkaConsumer
            .Setup(c => c.Consume(It.IsAny<TimeSpan>()))
            .Throws(new ConsumeException(new ConsumeResult<byte[], byte[]>(), new Error(ErrorCode.UnknownTopicOrPart)));

        // Act
        var result = await ConsumeMessageAsync(service, kafkaConsumer.Object);

        // Assert
        Assert.Null(result);
    }

    // ---- BuildRequestItems ----

    [Fact]
    public void BuildRequestItems_ShouldMapRegisterFields_ForRegisterAction()
    {
        // Arrange
        var messages = new List<TrackingActionMessage>
        {
            new() { tracking_number = "TN1", carrier = 100, auto_detect = true }
        };

        // Act
        var items = BuildRequestItems("register", messages);
        var json = JsonSerializer.Serialize(items[0]);

        // Assert
        Assert.Contains("\"number\":\"TN1\"", json);
        Assert.Contains("\"carrier\":100", json);
        Assert.Contains("\"auto_detect\":true", json);
    }

    [Fact]
    public void BuildRequestItems_ShouldMapOldAndNewCarrier_ForChangeCarrierAction()
    {
        // Arrange
        var messages = new List<TrackingActionMessage>
        {
            new() { tracking_number = "TN1", carrier = 200, carrier_old = 100 }
        };

        // Act
        var items = BuildRequestItems("changecarrier", messages);
        var json = JsonSerializer.Serialize(items[0]);

        // Assert
        Assert.Contains("\"number\":\"TN1\"", json);
        Assert.Contains("\"carrier_old\":100", json);
        Assert.Contains("\"carrier_new\":200", json);
    }

    [Fact]
    public void BuildRequestItems_ShouldMapTagAndNote_ForChangeInfoAction()
    {
        // Arrange
        var messages = new List<TrackingActionMessage>
        {
            new() { tracking_number = "TN1", tag = "my-tag", note = "my-note", auto_detect = false }
        };

        // Act
        var items = BuildRequestItems("changeinfo", messages);
        var json = JsonSerializer.Serialize(items[0]);

        // Assert
        Assert.Contains("\"number\":\"TN1\"", json);
        Assert.Contains("\"tag\":\"my-tag\"", json);
        Assert.Contains("\"note\":\"my-note\"", json);
        Assert.Contains("\"auto_detect\":false", json);
    }

    [Theory]
    [InlineData("stoptrack")]
    [InlineData("retrack")]
    [InlineData("deletetrack")]
    [InlineData("push")]
    public void BuildRequestItems_ShouldMapNumberAndCarrierOnly_ForSimpleActions(string action)
    {
        // Arrange
        var messages = new List<TrackingActionMessage>
        {
            new() { tracking_number = "TN1", carrier = 100 }
        };

        // Act
        var items = BuildRequestItems(action, messages);
        var json = JsonSerializer.Serialize(items[0]);

        // Assert
        Assert.Contains("\"number\":\"TN1\"", json);
        Assert.Contains("\"carrier\":100", json);
        Assert.DoesNotContain("carrier_old", json);
        Assert.DoesNotContain("auto_detect", json);
    }

    // ---- Call17TrackAsync ----

    [Fact]
    public async Task Call17TrackAsync_ShouldMapAcceptedAndRejectedItems_ToResults()
    {
        // Arrange
        var service = CreateService();
        var messages = new List<TrackingActionMessage>
        {
            new() { tracking_number = "TN1", carrier = 100 },
            new() { tracking_number = "TN2", carrier = 100 }
        };
        Uri? capturedUri = null;
        string? capturedApiToken = null;
        string? capturedBody = null;
        SetupHttpResponse(HttpStatusCode.OK, """
        {
            "code": 0,
            "data": {
                "accepted": [ { "number": "TN1", "carrier": 100 } ],
                "rejected": [ { "number": "TN2", "carrier": 100, "error": { "code": 4031, "message": "Not Found" } } ]
            }
        }
        """, req =>
        {
            // Content is disposed by the "using" block in Call17TrackAsync right after the call
            // returns, so it must be read synchronously here, while the request is still alive.
            capturedUri = req.RequestUri;
            capturedApiToken = req.Headers.GetValues("17token").Single();
            capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
        });

        // Act
        var results = await Call17TrackAsync(service, "register", "/register", "test-key", messages);

        // Assert
        Assert.Equal(2, results.Length);
        var tn1 = Assert.Single(results, r => r.tracking_number == "TN1");
        Assert.True(tn1.success);
        Assert.Null(tn1.error_code);
        var tn2 = Assert.Single(results, r => r.tracking_number == "TN2");
        Assert.False(tn2.success);
        Assert.Equal(4031, tn2.error_code);

        Assert.Equal("https://track17.test/register", capturedUri!.ToString());
        Assert.Equal("test-key", capturedApiToken);
        Assert.Contains("\"number\":\"TN1\"", capturedBody);
    }

    [Fact]
    public async Task Call17TrackAsync_ShouldReturnAllFailed_WhenHttpStatusIsNotSuccess()
    {
        // Arrange
        var service = CreateService();
        var messages = new List<TrackingActionMessage>
        {
            new() { tracking_number = "TN1", carrier = 100 },
            new() { tracking_number = "TN2", carrier = 200 }
        };
        SetupHttpResponse(HttpStatusCode.InternalServerError, "error");

        // Act
        var results = await Call17TrackAsync(service, "register", "/register", "test-key", messages);

        // Assert
        Assert.Equal(2, results.Length);
        Assert.All(results, r => Assert.False(r.success));
        Assert.All(results, r => Assert.Null(r.error_code));
    }

    [Fact]
    public async Task Call17TrackAsync_ShouldReturnAllFailed_WhenHttpRequestThrows()
    {
        // Arrange
        var service = CreateService();
        var messages = new List<TrackingActionMessage>
        {
            new() { tracking_number = "TN1", carrier = 100 }
        };
        SetupHttpThrows(new HttpRequestException("connection refused"));

        // Act
        var results = await Call17TrackAsync(service, "register", "/register", "test-key", messages);

        // Assert
        var result = Assert.Single(results);
        Assert.False(result.success);
    }

    [Fact]
    public async Task Call17TrackAsync_ShouldReturnAllFailed_WhenResponseJsonIsInvalid()
    {
        // Arrange
        var service = CreateService();
        var messages = new List<TrackingActionMessage>
        {
            new() { tracking_number = "TN1", carrier = 100 }
        };
        SetupHttpResponse(HttpStatusCode.OK, "not-json");

        // Act
        var results = await Call17TrackAsync(service, "register", "/register", "test-key", messages);

        // Assert
        var result = Assert.Single(results);
        Assert.False(result.success);
    }

    [Fact(Timeout = 15000)]
    public async Task SyncAllAsync_ShouldSkipEveryTopic_WhenKafkaBrokerIsUnreachable()
    {
        // ConsumeBatch builds its own real Confluent.Kafka IConsumer internally (not
        // injected/mockable), so this exercises the full SyncAllAsync -> SyncTopicAsync ->
        // ConsumeBatch orchestration loop end-to-end against an unreachable broker (127.0.0.1:1,
        // connection refused) rather than mocking pieces of it. A short ConsumeTimeoutMs keeps the
        // 5-empty-poll bail-out bounded (~5 * 100ms per topic * 5 topics), so this stays fast and
        // deterministic instead of depending on librdkafka's multi-minute default message timeout.
        // Arrange
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Track17:ApiKey"] = "test-key",
            ["Kafka:BootstrapServers"] = "127.0.0.1:1",
            ["Track17:ConsumeTimeoutMs"] = "100"
        });

        // Act
        var exception = await Record.ExceptionAsync(() => service.SyncAllAsync());

        // Assert
        Assert.Null(exception);
        _httpClientFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
        _kafka.Verify(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
