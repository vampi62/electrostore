namespace ElectrostoreCRON.Services.StockLowAlertService;

public interface IStockLowAlertService
{
    /// <summary>
    /// Récupère les items dont la quantité est passée sous leur seuil minimum auprès de l'API
    /// et publie un message de notification par administrateur sur le topic "notification-requests".
    /// </summary>
    /// <param name="paramsJson">Paramètres JSON du cron job (voir <c>StockLowAlertParams</c>).</param>
    /// <param name="lastRunAt">Date de dernier lancement du cron job (colonne <c>last_run_at</c>), utilisée comme
    /// point de départ de la fenêtre "changements récents" lorsque <c>use_last_run</c> vaut
    /// <see langword="true"/> dans les paramètres.</param>
    Task SendAlertAsync(string? paramsJson, DateTime? lastRunAt, CancellationToken ct = default);
}
