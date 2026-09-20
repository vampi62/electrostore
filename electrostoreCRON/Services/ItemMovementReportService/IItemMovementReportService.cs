namespace ElectrostoreCRON.Services.ItemMovementReportService;

public interface IItemMovementReportService
{
    /// <summary>
    /// Retrieves the item movements over the period from the API and publishes one
    /// notification message per administrator on the "notification-requests" topic.
    /// </summary>
    /// <param name="paramsJson">JSON parameters of the cron job (see <c>WeeklyReportParams</c>).</param>
    /// <param name="lastRunAt">Date of the cron job's last run (<c>last_run_at</c> column), used as the
    /// start of the period when <c>use_last_run</c> is <see langword="true"/> in the parameters.</param>
    Task SendReportAsync(string? paramsJson, DateTime? lastRunAt, CancellationToken ct = default);
}
