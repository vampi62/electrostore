using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using ElectrostoreCRON.Kafka.Messages;
using ElectrostoreCRON.Kafka.Producer;

namespace ElectrostoreCRON.Services.Track17SyncService;

public class Track17SyncService : ITrack17SyncService
{
    private const string ResultTopic = "tracking-result";
    private readonly int    _batchSize;
    private readonly string _track17Base;
    private readonly int    _consumeTimeoutMs;

    private static readonly (string topic, string action, string endpoint)[] ActionMap =
    [
        ("tracking-request-add",    "register",       "/register"),
        ("tracking-request-change", "changecarrier",  "/changecarrier"),
        ("tracking-request-stop",   "stoptrack",      "/stoptrack"),
        ("tracking-request-resume", "retrack",        "/retrack"),
        ("tracking-request-delete", "deletetrack",    "/deletetrack"),
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IKafkaProducerService       _kafka;
    private readonly IHttpClientFactory          _httpClientFactory;
    private readonly IConfiguration              _configuration;
    private readonly ILogger<Track17SyncService> _logger;

    public Track17SyncService(
        IKafkaProducerService kafka,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<Track17SyncService> logger)
    {
        _kafka             = kafka;
        _httpClientFactory = httpClientFactory;
        _configuration     = configuration;
        _logger            = logger;

        _batchSize        = _configuration.GetValue<int>("Track17:BatchSize", 40);
        _track17Base      = _configuration["Track17:BaseUrl"] ?? "https://api.17track.net/track/v2.4";
        _consumeTimeoutMs = _configuration.GetValue<int>("Track17:ConsumeTimeoutMs", 300);
    }

    public async Task SyncAllAsync(CancellationToken ct = default)
    {
        var apiKey = _configuration.GetValue<string>("Track17:ApiKey");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Track17:ApiKey not configured - sync skipped.");
            return;
        }

        foreach (var (topic, action, endpoint) in ActionMap)
        {
            if (ct.IsCancellationRequested) break;
            await SyncTopicAsync(topic, action, endpoint, apiKey, ct);
        }
    }

    // -------------------------------------------------------------------------

    private async Task SyncTopicAsync(
        string topic, string action, string endpoint, string apiKey, CancellationToken ct)
    {
        var messages = ConsumeBatch(topic);
        if (messages.Count == 0)
        {
            _logger.LogDebug("Track17 sync: topic={Topic} - no pending messages.", topic);
            return;
        }

        _logger.LogInformation("Track17 sync: topic={Topic} action={Action} count={Count}",
            topic, action, messages.Count);

        var results = await Call17TrackAsync(action, endpoint, apiKey, messages, ct);

        foreach (var result in results)
        {
            await _kafka.PublishAsync(
                ResultTopic,
                $"{result.tracking_number}_{result.carrier}",
                JsonSerializer.Serialize(result, JsonOptions),
                ct);
        }

        _logger.LogInformation(
            "Track17 sync: {Ok}/{Total} results published to {ResultTopic} (action={Action})",
            results.Count(r => r.success), results.Length, ResultTopic, action);
    }

    // ---- Batch consumption from Kafka ----------------------------------------

    private List<TrackingActionMessage> ConsumeBatch(string topic)
    {
        var bootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "kafka:9092";
        var groupId = (_configuration["Kafka:ConsumerGroupId"] ?? "cron-service") + "-17track";

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId          = groupId,
            AutoOffsetReset  = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(topic);

        var messages          = new List<TrackingActionMessage>(_batchSize);
        ConsumeResult<string, string>? lastCommittable = null;

        try
        {
            while (messages.Count < _batchSize)
            {
                var result = consumer.Consume(TimeSpan.FromMilliseconds(_consumeTimeoutMs));
                if (result is null || result.IsPartitionEOF)
                    break;

                TrackingActionMessage? msg;
                try
                {
                    msg = JsonSerializer.Deserialize<TrackingActionMessage>(result.Message.Value, JsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex,
                        "Track17 sync: invalid JSON in topic={Topic}, offset={Offset} - skipped.",
                        topic, result.Offset);
                    lastCommittable = result;
                    continue;
                }

                if (msg is null || string.IsNullOrWhiteSpace(msg.tracking_number))
                {
                    _logger.LogWarning(
                        "Track17 sync: incomplete message in topic={Topic}, offset={Offset} - skipped.",
                        topic, result.Offset);
                    lastCommittable = result;
                    continue;
                }

                messages.Add(msg);
                lastCommittable = result;
            }

            if (lastCommittable is not null)
                consumer.Commit(lastCommittable);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Track17 sync: consumption error in topic={Topic}.", topic);
        }
        finally
        {
            consumer.Close();
        }

        return messages;
    }

    // ---- 17track API call ------------------------------------------------------

    private async Task<TrackingResultMessage[]> Call17TrackAsync(
        string action, string endpoint, string apiKey,
        List<TrackingActionMessage> messages, CancellationToken ct)
    {
        var requestItems = BuildRequestItems(action, messages);
        var requestBody  = JsonSerializer.Serialize(requestItems, JsonOptions);

        using var client  = _httpClientFactory.CreateClient();
        using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
        client.DefaultRequestHeaders.Add("17token", apiKey);

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(_track17Base + endpoint, content, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Track17 sync: HTTP error calling endpoint={Endpoint}.", endpoint);
            return BuildErrorResults(action, messages);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Track17 sync: endpoint={Endpoint} HTTP {Code}.",
                endpoint, (int)response.StatusCode);
            return BuildErrorResults(action, messages);
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        Track17BatchResponse? apiResp;
        try
        {
            apiResp = JsonSerializer.Deserialize<Track17BatchResponse>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Track17 sync: response parsing failed for endpoint={Endpoint}.", endpoint);
            return BuildErrorResults(action, messages);
        }

        return BuildResults(action, messages, apiResp);
    }

    // ---- Request item builders -----------------------------------------------

    private static object[] BuildRequestItems(string action, List<TrackingActionMessage> messages) =>
        action switch
        {
            "register" => messages.Select(m => (object)new RegisterItem(
                m.tracking_number,
                m.carrier,
                m.auto_detect)).ToArray(),

            "changecarrier" => messages.Select(m => (object)new ChangeCarrierItem(
                m.tracking_number,
                m.carrier_old ?? 0,
                m.carrier ?? 0)).ToArray(),

            "changeinfo" => messages.Select(m => (object)new ChangeInfoItem(
                m.tracking_number,
                m.tag,
                m.note,
                m.auto_detect)).ToArray(),

            // stoptrack, retrack, deletetrack, push - number + carrier payload
            _ => messages.Select(m => (object)new SimpleItem(m.tracking_number, m.carrier)).ToArray(),
        };

    // ---- Result builders ----------------------------------------------------------

    private TrackingResultMessage[] BuildResults(
        string action, List<TrackingActionMessage> messages, Track17BatchResponse? apiResp)
    {
        var acceptedMap = apiResp?.data?.accepted?
            .ToDictionary(a => a.number, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, Track17AcceptedItem>(StringComparer.OrdinalIgnoreCase);

        var rejectedMap = apiResp?.data?.rejected?
            .ToDictionary(r => r.number, r => r.error?.code, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);

        return messages.Select(m =>
        {
            var isAccepted = acceptedMap.TryGetValue(m.tracking_number, out var acc);
            var errorCode  = rejectedMap.TryGetValue(m.tracking_number, out var err) ? err : null;

            return new TrackingResultMessage
            {
                action          = action,
                tracking_number = m.tracking_number,
                carrier         = m.carrier ?? 0,
                carrier_old     = m.carrier_old,
                success         = isAccepted,
                error_code      = errorCode,
            };
        }).ToArray();
    }

    private static TrackingResultMessage[] BuildErrorResults(
        string action, List<TrackingActionMessage> messages) =>
        messages.Select(m => new TrackingResultMessage
        {
            action          = action,
            tracking_number = m.tracking_number,
            carrier         = m.carrier ?? 0,
            success         = false,
        }).ToArray();

    // ---- 17track request models -------------------------------------------------

    private sealed record RegisterItem(
        [property: JsonPropertyName("number")]      string  number,
        [property: JsonPropertyName("carrier")]     int?    carrier,
        [property: JsonPropertyName("auto_detect")] bool?   auto_detect);

    private sealed record ChangeCarrierItem(
        [property: JsonPropertyName("number")]      string number,
        [property: JsonPropertyName("carrier_old")] int    carrier_old,
        [property: JsonPropertyName("carrier_new")] int    carrier_new);

    private sealed record ChangeInfoItem(
        [property: JsonPropertyName("number")]      string  number,
        [property: JsonPropertyName("tag")]         string? tag,
        [property: JsonPropertyName("note")]        string? note,
        [property: JsonPropertyName("auto_detect")] bool?   auto_detect);

    private sealed record SimpleItem(
        [property: JsonPropertyName("number")]  string number,
        [property: JsonPropertyName("carrier")] int?   carrier);

    // ---- 17track response models ------------------------------------------------

    private sealed record Track17BatchResponse(int code, Track17BatchData? data);

    private sealed record Track17BatchData(
        Track17AcceptedItem[]? accepted,
        Track17RejectedItem[]? rejected);

    private sealed record Track17AcceptedItem(
        [property: JsonPropertyName("origin")]        int?          origin,
        [property: JsonPropertyName("number")]        string        number,
        [property: JsonPropertyName("carrier")]       int?          carrier,
        [property: JsonPropertyName("email")]         string?       email,
        [property: JsonPropertyName("lang")]          string?       lang,
        [property: JsonPropertyName("final_carrier")] int?          final_carrier);

    private sealed record Track17Error(
        [property: JsonPropertyName("code")]    int    code,
        [property: JsonPropertyName("message")] string message);

    private sealed record Track17RejectedItem(
        [property: JsonPropertyName("number")]  string        number,
        [property: JsonPropertyName("carrier")] int           carrier,
        [property: JsonPropertyName("error")]   Track17Error? error);
}
