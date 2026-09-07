namespace ElectrostoreCRON.Services.ItemMovementReportService;

public interface IItemMovementReportService
{
    /// <summary>
    /// Récupère les mouvements d'items de la période auprès de l'API et publie un
    /// message de notification par administrateur sur le topic "notification-requests".
    /// </summary>
    /// <param name="paramsJson">Paramètres JSON du cron job (voir <c>WeeklyReportParams</c>).</param>
    /// <param name="lastRunAt">Date de dernier lancement du cron job (colonne <c>last_run_at</c>), utilisée comme
    /// début de période lorsque <c>use_last_run</c> vaut <see langword="true"/> dans les paramètres.</param>
    Task SendReportAsync(string? paramsJson, DateTime? lastRunAt, CancellationToken ct = default);
}
