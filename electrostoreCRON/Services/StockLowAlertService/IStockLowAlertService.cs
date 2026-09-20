namespace ElectrostoreCRON.Services.StockLowAlertService;

public interface IStockLowAlertService
{
    /// <summary>
    /// Retrieves the items whose quantity has dropped below their minimum threshold from the API
    /// and publishes one notification message per administrator on the "notification-requests" topic.
    /// </summary>
    /// <param name="paramsJson">JSON parameters of the cron job (see <c>StockLowAlertParams</c>).</param>
    /// <param name="lastRunAt">Date of the cron job's last run (<c>last_run_at</c> column), used as the
    /// starting point of the "recent changes" window when <c>use_last_run</c> is
    /// <see langword="true"/> in the parameters.</param>
    Task SendAlertAsync(string? paramsJson, DateTime? lastRunAt, CancellationToken ct = default);
}
