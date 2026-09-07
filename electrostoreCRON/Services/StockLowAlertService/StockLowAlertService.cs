using System.Text.Json;
using System.Text.Json.Serialization;
using ElectrostoreCRON.Grpc;
using ElectrostoreCRON.Kafka.Messages;
using ElectrostoreCRON.Kafka.Producer;
using Grpc.Core;

namespace ElectrostoreCRON.Services.StockLowAlertService;

public class StockLowAlertService : IStockLowAlertService
{
    private const string NotificationTopic = "notification-requests";
    private const string TemplateId = "stock-low-alert";
    private const int DefaultRecentChangeDays = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly ItemsGrpc.ItemsGrpcClient _apiClient;
    private readonly IKafkaProducerService _kafka;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StockLowAlertService> _logger;

    public StockLowAlertService(
        ItemsGrpc.ItemsGrpcClient apiClient,
        IKafkaProducerService kafka,
        IConfiguration configuration,
        ILogger<StockLowAlertService> logger)
    {
        _apiClient     = apiClient;
        _kafka         = kafka;
        _configuration = configuration;
        _logger        = logger;
    }

    public async Task SendAlertAsync(string? paramsJson, DateTime? lastRunAt, CancellationToken ct = default)
    {
        var parameters = ParseParams(paramsJson);
        var language   = parameters.language ?? _configuration["AppLanguage"] ?? "fr";
        List<string> types = parameters.types is { Count: > 0 } ? parameters.types : ["email"];
        var sinceDate  = ResolveSinceDate(parameters, lastRunAt);

        GetLowStockItemsReply report;
        try
        {
            report = await _apiClient.GetLowStockItemsAsync(new GetLowStockItemsRequest
            {
                SinceDate = sinceDate?.ToString("O") ?? string.Empty,
            }, cancellationToken: ct);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Stock low alert: failed to retrieve low stock items from the API.");
            return;
        }

        _logger.LogInformation(
            "Stock low alert: {ItemCount} item(s) below threshold for {RecipientCount} administrator(s).",
            report.Items.Count, report.Recipients.Count);

        if (report.Items.Count == 0)
        {
            _logger.LogInformation("Stock low alert: no item below its threshold - no notification sent.");
            return;
        }
        if (report.Recipients.Count == 0)
        {
            _logger.LogWarning("Stock low alert: no administrator to notify - no notification sent.");
            return;
        }

        var items = report.Items.Select(i => new LowStockItemRow
        {
            item           = string.IsNullOrEmpty(i.FriendlyNameItem) ? $"#{i.IdItem}" : i.FriendlyNameItem,
            reference      = i.ReferenceNameItem,
            quantity       = i.QuantityItem,
            threshold      = i.ThresholdMinItem,
        }).ToList();

        foreach (var recipient in report.Recipients)
        {
            if (ct.IsCancellationRequested) break;
            await PublishNotificationAsync(recipient, items, language, types, ct);
        }
    }

    // -------------------------------------------------------------------------

    private static DateTime? ResolveSinceDate(StockLowAlertParams parameters, DateTime? lastRunAt)
    {
        if (!parameters.only_recent_changes)
        {
            return null;
        }
        if (parameters.use_last_run && lastRunAt.HasValue)
        {
            return lastRunAt.Value;
        }
        var days = parameters.days is > 0 ? parameters.days.Value : DefaultRecentChangeDays;
        return DateTime.UtcNow.AddDays(-days);
    }

    private static StockLowAlertParams ParseParams(string? paramsJson)
    {
        if (string.IsNullOrWhiteSpace(paramsJson))
        {
            return new StockLowAlertParams();
        }
        try
        {
            return JsonSerializer.Deserialize<StockLowAlertParams>(paramsJson, JsonOptions) ?? new StockLowAlertParams();
        }
        catch (JsonException)
        {
            return new StockLowAlertParams();
        }
    }

    private async Task PublishNotificationAsync(
        LowStockRecipientItem recipient,
        List<LowStockItemRow> items,
        string language,
        List<string> types,
        CancellationToken ct)
    {
        var notification = new NotificationMessage
        {
            Types           = types,
            RecipientEmail  = recipient.Email,
            RecipientUserId = recipient.IdUser,
            TemplateId      = TemplateId,
            Language        = language,
            TemplateValues  = new Dictionary<string, object>
            {
                ["firstName"]  = recipient.Firstname,
                ["lastName"]   = recipient.Name,
                ["itemCount"]  = items.Count,
                ["items"]      = items,
            },
        };
        try
        {
            await _kafka.PublishAsync(
                NotificationTopic,
                $"{recipient.Email}-{TemplateId}",
                JsonSerializer.Serialize(notification, JsonOptions),
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stock low alert: unable to notify {Email}.", recipient.Email);
        }
    }

    // ---- Modèles ------------------------------------------------------------

    /// <summary>Contenu attendu de <c>params_cronjob</c> pour l'action StockLowAlert.</summary>
    private sealed record StockLowAlertParams
    {
        /// <summary>Langue des templates ("fr" / "en") ; à défaut, AppLanguage.</summary>
        public string? language { get; init; }

        /// <summary>Canaux de notification ("email", "webpush") ; "email" par défaut.</summary>
        public List<string>? types { get; init; }

        /// <summary>
        /// <see langword="false"/> (par défaut) : résumé de tous les items sous leur seuil minimum.
        /// <see langword="true"/> : ne retenir que les items ayant eu un changement de quantité
        /// récent (via ItemsHistory), sur la fenêtre définie par <c>use_last_run</c> / <c>days</c>.
        /// </summary>
        public bool only_recent_changes { get; init; }

        /// <summary>
        /// Lorsque <c>only_recent_changes</c> est actif, utiliser la date du dernier lancement du
        /// cron job (colonne <c>last_run_at</c>) comme début de la fenêtre "changements récents"
        /// plutôt que <c>days</c>. Sans exécution précédente (premier lancement), <c>days</c> sert
        /// de repli.
        /// </summary>
        public bool use_last_run { get; init; }

        /// <summary>
        /// Profondeur de la fenêtre "changements récents", en jours (1 par défaut). Utilisée
        /// uniquement lorsque <c>only_recent_changes</c> est actif et que <c>use_last_run</c> ne
        /// s'applique pas (désactivé ou premier lancement).
        /// </summary>
        public int? days { get; init; }
    }

    private sealed record LowStockItemRow
    {
        public required string item { get; init; }
        public required string reference { get; init; }
        public int quantity { get; init; }
        public int threshold { get; init; }
    }
}
