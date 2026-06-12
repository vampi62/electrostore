using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ElectrostoreCRON.DTO;
using ElectrostoreCRON.Kafka.Producer;

namespace ElectrostoreCRON.Services.ParcelTrackerService;

public class ParcelTrackerService : IParcelTrackerService
{
    private const string Topic            = "cron-parcel-tracking";
    private const string Track17ApiUrl    = "https://api.17track.net/track/v2.2/gettrackinfo";

    private readonly IKafkaProducerService _kafka;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ParcelTrackerService> _logger;

    public ParcelTrackerService(
        IKafkaProducerService kafka,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ParcelTrackerService> logger)
    {
        _kafka             = kafka;
        _httpClientFactory = httpClientFactory;
        _configuration     = configuration;
        _logger            = logger;
    }

    public async Task TrackAsync(string? paramsJson, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(paramsJson))
        {
            _logger.LogWarning("ParcelTracker: params_cronjob is empty, action skipped.");
            return;
        }

        ParcelTrackingParams? p;
        try
        {
            p = JsonSerializer.Deserialize<ParcelTrackingParams>(paramsJson);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "ParcelTracker: failed to deserialize params_cronjob: {Json}", paramsJson);
            return;
        }

        if (p is null || p.id_command == 0 || string.IsNullOrWhiteSpace(p.tracking_number))
        {
            _logger.LogWarning("ParcelTracker: incomplete params (id_command={Id}, tracking={Tracking}).",
                p?.id_command, p?.tracking_number);
            return;
        }

        var result = await FetchTrackingStatusAsync(p, ct);
        if (result is null)
        {
            _logger.LogWarning("ParcelTracker: failed to fetch tracking status for command {Id}.", p.id_command);
            return;
        }

        await _kafka.PublishAsync(Topic, p.id_command.ToString(), JsonSerializer.Serialize(result), ct);
        _logger.LogInformation(
            "ParcelTracker: tracking published for command {Id} — status={Status}, delivered={Delivered}",
            p.id_command, result.tracking_status, result.is_delivered);
    }

    private async Task<ParcelTrackingMessage?> FetchTrackingStatusAsync(
        ParcelTrackingParams p, CancellationToken ct)
    {
        var apiKey = _configuration["Track17:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning(
                "ParcelTracker: Track17:ApiKey not configured. Tracking skipped for command {Id}.", p.id_command);
            return null;
        }

        var requestBody = JsonSerializer.Serialize(new[]
        {
            new { number = p.tracking_number }
        });

        using var client  = _httpClientFactory.CreateClient();
        using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
        client.DefaultRequestHeaders.Add("17token", apiKey);

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(Track17ApiUrl, content, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ParcelTracker: HTTP error calling 17track for command {Id}.", p.id_command);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "ParcelTracker: 17track returned HTTP {Code} for command {Id}.",
                (int)response.StatusCode, p.id_command);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        Track17Response? apiResp;
        try
        {
            apiResp = JsonSerializer.Deserialize<Track17Response>(json);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "ParcelTracker: failed to parse 17track response for command {Id}.", p.id_command);
            return null;
        }

        if (apiResp?.data?.accepted is null || apiResp.data.accepted.Length == 0)
        {
            _logger.LogWarning(
                "ParcelTracker: 17track has no accepted result for tracking '{Num}' (command {Id}).",
                p.tracking_number, p.id_command);
            return null;
        }

        var track = apiResp.data.accepted[0].track;
        var (status, isDelivered) = MapCarrierStatus(track?.w1 ?? 0);

        var latestEvent     = track?.z0;
        var trackingEventTxt = latestEvent?.b ?? string.Empty;
        var dateLivraison   = isDelivered && latestEvent is not null ? latestEvent.a : string.Empty;

        return new ParcelTrackingMessage
        {
            action          = "track_parcel",
            id_command      = p.id_command,
            tracking_number = p.tracking_number,
            carrier         = p.carrier,
            tracking_status = status,
            tracking_event  = trackingEventTxt,
            is_delivered    = isDelivered,
            date_livraison  = dateLivraison,
        };
    }

    /// <summary>
    /// Convertit le code de statut 17track (w1) vers un statut interne.
    /// https://res.17track.net/asset/carrier/info/apifileinfo_en.zip → TrackingStatusCode
    /// </summary>
    private static (string status, bool isDelivered) MapCarrierStatus(int w1) => w1 switch
    {
        0  => ("not_found",          false),
        10 => ("in_transit",         false),
        20 => ("expired",            false),
        30 => ("pickup",             false),
        35 => ("undelivered",        false),
        40 => ("delivered",          true),
        50 => ("delivery_exception", false),
        _  => ("in_transit",         false),
    };

    // ---- modèles de désérialisation 17track ----

    private sealed record Track17Response(int code, Track17Data? data);

    private sealed record Track17Data(Track17Accepted[]? accepted, Track17Rejected[]? rejected);

    private sealed record Track17Accepted(string number, Track17Track? track);

    private sealed record Track17Rejected(string number, int error);

    private sealed record Track17Track(
        int? w1,            // statut principal
        int? w2,            // sous-statut
        Track17Event? z0,   // dernier événement
        Track17Event[]? z1  // tous les événements
    );

    private sealed record Track17Event(
        string a,   // date/heure
        string b,   // description
        string? c   // localisation
    );
}

