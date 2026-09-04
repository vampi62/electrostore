namespace ElectrostoreCRON.Services.ItemMovementReportService;

public interface IItemMovementReportService
{
    /// <summary>
    /// Récupère les mouvements d'items de la période auprès de l'API et publie un
    /// message de notification par administrateur sur le topic "notification-requests".
    /// </summary>
    /// <param name="paramsJson">Paramètres JSON du cron job (voir <c>WeeklyReportParams</c>).</param>
    Task SendReportAsync(string? paramsJson, CancellationToken ct = default);
}
