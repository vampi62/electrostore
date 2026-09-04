using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using ElectrostoreCRON.Grpc;
using ElectrostoreCRON.Kafka.Messages;
using ElectrostoreCRON.Kafka.Producer;
using Grpc.Core;

namespace ElectrostoreCRON.Services.ItemMovementReportService;

public class ItemMovementReportService : IItemMovementReportService
{
    private const string NotificationTopic = "notification-requests";
    private const string TemplateId = "weekly-item-movement-report";
    private const int DefaultPeriodDays = 7;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Libellés des ItemHistoryType par langue, l'anglais servant de repli.</summary>
    private static readonly Dictionary<string, Dictionary<string, string>> TypeLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ItemCreated"]  = "Item created",
            ["ItemUpdated"]  = "Item updated",
            ["ItemDeleted"]  = "Item deleted",
            ["StockAdded"]   = "Stock added",
            ["StockRemoved"] = "Stock removed",
            ["StockUpdated"] = "Stock updated",
        },
        ["fr"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ItemCreated"]  = "Article créé",
            ["ItemUpdated"]  = "Article modifié",
            ["ItemDeleted"]  = "Article supprimé",
            ["StockAdded"]   = "Stock ajouté",
            ["StockRemoved"] = "Stock retiré",
            ["StockUpdated"] = "Stock ajusté",
        },
    };

    private readonly ItemsHistoryGrpc.ItemsHistoryGrpcClient _apiClient;
    private readonly IKafkaProducerService _kafka;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ItemMovementReportService> _logger;

    public ItemMovementReportService(
        ItemsHistoryGrpc.ItemsHistoryGrpcClient apiClient,
        IKafkaProducerService kafka,
        IConfiguration configuration,
        ILogger<ItemMovementReportService> logger)
    {
        _apiClient     = apiClient;
        _kafka         = kafka;
        _configuration = configuration;
        _logger        = logger;
    }

    public async Task SendReportAsync(string? paramsJson, CancellationToken ct = default)
    {
        var parameters = ParseParams(paramsJson);
        var language   = parameters.language ?? _configuration["AppLanguage"] ?? "fr";
        var days       = parameters.days is > 0 ? parameters.days.Value : DefaultPeriodDays;
        List<string> types = parameters.types is { Count: > 0 } ? parameters.types : ["email"];

        var toDate   = DateTime.UtcNow;
        var fromDate = toDate.AddDays(-days);

        GetItemsMovementReportReply report;
        try
        {
            report = await _apiClient.GetItemsMovementReportAsync(new GetItemsMovementReportRequest
            {
                FromDate = fromDate.ToString("O"),
                ToDate   = toDate.ToString("O"),
            }, cancellationToken: ct);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Item movement report: failed to retrieve the history from the API.");
            return;
        }

        _logger.LogInformation(
            "Item movement report: {MovementCount} movement(s) over {Days} day(s) for {RecipientCount} administrator(s).",
            report.Movements.Count, days, report.Recipients.Count);

        if (report.Movements.Count == 0 && !parameters.send_when_empty)
        {
            _logger.LogInformation("Item movement report: no movement over the period - no notification sent.");
            return;
        }
        if (report.Recipients.Count == 0)
        {
            _logger.LogWarning("Item movement report: no administrator to notify - no notification sent.");
            return;
        }

        var movements = BuildMovementRows(report, language);

        foreach (var recipient in report.Recipients)
        {
            if (ct.IsCancellationRequested) break;
            await PublishNotificationAsync(recipient, movements, report, language, types, ct);
        }
    }

    // -------------------------------------------------------------------------

    private static WeeklyReportParams ParseParams(string? paramsJson)
    {
        if (string.IsNullOrWhiteSpace(paramsJson))
        {
            return new WeeklyReportParams();
        }
        try
        {
            return JsonSerializer.Deserialize<WeeklyReportParams>(paramsJson, JsonOptions) ?? new WeeklyReportParams();
        }
        catch (JsonException)
        {
            return new WeeklyReportParams();
        }
    }

    private static List<MovementRow> BuildMovementRows(GetItemsMovementReportReply report, string language)
    {
        var labels = TypeLabels.TryGetValue(language, out var localized) ? localized : TypeLabels["en"];
        return report.Movements.Select(m => new MovementRow
        {
            date            = FormatDate(m.CreatedAt, "yyyy-MM-dd HH:mm"),
            item            = string.IsNullOrEmpty(m.ItemName) ? $"#{m.IdItem}" : m.ItemName,
            type            = labels.TryGetValue(m.Type, out var label) ? label : m.Type,
            quantityChange  = m.QuantityChange > 0 ? $"+{m.QuantityChange}" : m.QuantityChange.ToString(CultureInfo.InvariantCulture),
            oldQuantity     = m.OldQuantity,
            newQuantity     = m.NewQuantity,
            user            = string.IsNullOrWhiteSpace(m.UserName) ? "-" : m.UserName,
            notes           = m.Notes,
        }).ToList();
    }

    private async Task PublishNotificationAsync(
        ReportRecipientItem recipient,
        List<MovementRow> movements,
        GetItemsMovementReportReply report,
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
                ["firstName"]     = recipient.Firstname,
                ["lastName"]      = recipient.Name,
                ["fromDate"]      = FormatDate(report.FromDate, "yyyy-MM-dd"),
                ["toDate"]        = FormatDate(report.ToDate, "yyyy-MM-dd"),
                ["movementCount"] = movements.Count,
                ["movements"]     = movements,
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
            _logger.LogError(ex, "Item movement report: unable to notify {Email}.", recipient.Email);
        }
    }

    private static string FormatDate(string isoDate, string format) =>
        DateTime.TryParse(isoDate, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed.ToUniversalTime().ToString(format, CultureInfo.InvariantCulture)
            : isoDate;

    // ---- Modèles ----------------------------------------------------------------

    /// <summary>Contenu attendu de <c>params_cronjob</c> pour l'action WeeklyItemMovementReport.</summary>
    private sealed record WeeklyReportParams
    {
        /// <summary>Profondeur de la période, en jours (7 par défaut).</summary>
        public int? days { get; init; }

        /// <summary>Langue des templates ("fr" / "en") ; à défaut, AppLanguage.</summary>
        public string? language { get; init; }

        /// <summary>Canaux de notification ("email", "webpush") ; "email" par défaut.</summary>
        public List<string>? types { get; init; }

        /// <summary>Envoyer le rapport même si aucun mouvement n'a eu lieu.</summary>
        public bool send_when_empty { get; init; }
    }

    private sealed record MovementRow
    {
        public required string date { get; init; }
        public required string item { get; init; }
        public required string type { get; init; }
        public required string quantityChange { get; init; }
        public int oldQuantity { get; init; }
        public int newQuantity { get; init; }
        public required string user { get; init; }
        public required string notes { get; init; }
    }
}
