namespace ElectrostoreCRON.Services.StockLowAlertService;

public interface IStockLowAlertService
{
    /// <summary>
    /// Récupère les items dont la quantité est passée sous leur seuil minimum auprès de l'API
    /// et publie un message de notification par administrateur sur le topic "notification-requests".
    /// </summary>
    /// <param name="paramsJson">Paramètres JSON du cron job (voir <c>StockLowAlertParams</c>).</param>
    Task SendAlertAsync(string? paramsJson, CancellationToken ct = default);
}
