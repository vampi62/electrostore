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
        _logger.LogDebug("----------------------------------------------------------------------");
        _logger.LogDebug("Track17 sync: starting sync for {TopicCount} topics.", ActionMap.Length);
        _logger.LogDebug("Track17 sync: date={Date} - starting sync for {TopicCount} topics.",
            DateTime.UtcNow, ActionMap.Length);

        foreach (var (topic, action, endpoint) in ActionMap)
        {
            if (ct.IsCancellationRequested) break;
            await SyncTopicAsync(topic, action, endpoint, apiKey, ct);
        }
        _logger.LogDebug("----------------------------------------------------------------------");
    }

    // -------------------------------------------------------------------------

    private async Task SyncTopicAsync(
        string topic, string action, string endpoint, string apiKey, CancellationToken ct)
    {
        var messages = await ConsumeBatch(topic, ct);
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

    private async Task<List<TrackingActionMessage>> ConsumeBatch(string topic, CancellationToken ct)
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
        var messages          = new List<TrackingActionMessage>(_batchSize);
        ConsumeResult<string, string>? lastCommittable = null;

        _logger.LogInformation(
            "Track17 sync: consuming from topic={Topic} (group={Group}, servers={Servers})",
            topic, groupId, bootstrapServers);

        const int maxConsecutiveEmptyPolls = 5;
        var consecutiveEmptyPolls = 0;

        try
        {
            while (messages.Count < _batchSize && !ct.IsCancellationRequested)
            {
                var result = await ConsumeMessageAsync(consumer, ct);
                if (result is null)
                {
                    consecutiveEmptyPolls++;
                    _logger.LogDebug(
                        "Track17 sync: topic={Topic} - empty poll {Count}/{Max} (likely rebalance or no new message).",
                        topic, consecutiveEmptyPolls, maxConsecutiveEmptyPolls);
                    if (consecutiveEmptyPolls >= maxConsecutiveEmptyPolls)
                    {
                        break;
                    }
                    continue;
                }
                if (result.IsPartitionEOF)
                {
                    _logger.LogDebug("Track17 sync: topic={Topic} - end of partition reached.", topic);
                    break;
                }

                consecutiveEmptyPolls = 0;
                var msg = DeserializeMessage(result);
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
            {
                consumer.Commit(lastCommittable);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Track17 sync: consumption error in topic={Topic}.", topic);
        }
        finally
        {
            consumer.Close();
        }
        _logger.LogDebug("Track17 sync: topic={Topic} - consumed {Count} messages.", topic, messages.Count);
        _logger.LogDebug("Track17 messages: {Messages}", string.Join(", ", messages.Select(m => m.tracking_number)));
        return messages;
    }

    private async Task<ConsumeResult<string, string>?> ConsumeMessageAsync(
        IConsumer<string, string> consumer, 
        CancellationToken ct)
    {
        try
        {
            return await Task.Run(() => consumer.Consume(TimeSpan.FromMilliseconds(_consumeTimeoutMs)), ct);
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

    private TrackingActionMessage? DeserializeMessage(ConsumeResult<string, string> result)
    {
        try
        {
            return JsonSerializer.Deserialize<TrackingActionMessage>(result.Message.Value, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid Kafka message (JSON) - offset {Offset}", result.Offset);
            return null;
        }
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
            _logger.LogDebug("Track17 sync: sending POST request to {Url}.", _track17Base + endpoint);
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
        _logger.LogDebug("Track17 sync: endpoint={Endpoint} response: {Response}", endpoint, json);
        _logger.LogDebug("Track17 sync: endpoint={Endpoint} response json={ResponseJson}", endpoint, apiResp);
        _logger.LogDebug("Track17 sync: endpoint={Endpoint} accepted={AcceptedCount} rejected={RejectedCount}.",
            endpoint, apiResp?.data?.accepted?.Length ?? 0, apiResp?.data?.rejected?.Length ?? 0);

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

    private static TrackingResultMessage[] BuildResults(
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
            var isAccepted = acceptedMap.TryGetValue(m.tracking_number, out _);
            var errorCode  = rejectedMap.TryGetValue(m.tracking_number, out var code) ? code : null;
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
