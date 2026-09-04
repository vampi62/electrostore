using ElectrostoreCRON.Grpc;
using ElectrostoreCRON.Services.ItemMovementReportService;
using ElectrostoreCRON.Services.Track17SyncService;
using Grpc.Core;
using Quartz;

namespace ElectrostoreCRON.Services.CronSchedulerService;

[DisallowConcurrentExecution]
public class ElectrostoreCronJob : IJob
{
    public const string KeyAction = "action_cronjob";
    public const string KeyParams = "params_cronjob";
    public const string KeyId     = "id_cronjob";

    private readonly ITrack17SyncService           _track17Sync;
    private readonly IItemMovementReportService    _itemMovementReport;
    private readonly CronJobsGrpc.CronJobsGrpcClient _apiClient;
    private readonly ILogger<ElectrostoreCronJob>  _logger;

    public ElectrostoreCronJob(
        ITrack17SyncService track17Sync,
        IItemMovementReportService itemMovementReport,
        CronJobsGrpc.CronJobsGrpcClient apiClient,
        ILogger<ElectrostoreCronJob> logger)
    {
        _track17Sync        = track17Sync;
        _itemMovementReport = itemMovementReport;
        _apiClient          = apiClient;
        _logger             = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var map    = context.JobDetail.JobDataMap;
        var action = Enum.TryParse<CronJobAction>(map.Get(KeyAction)?.ToString(), out var actionValue) ? (int)actionValue : -1;
        var id     = map.GetInt(KeyId);
        var jobParams = map.GetString(KeyParams);

        _logger.LogInformation("Running cron job #{Id} - action={Action}", id, action);

        try
        {
            switch (action)
            {
                case (int)CronJobAction.PackageTracking:
                    await _track17Sync.SyncAllAsync(context.CancellationToken);
                    break;

                case (int)CronJobAction.WeeklyItemMovementReport:
                    await _itemMovementReport.SendReportAsync(jobParams, context.CancellationToken);
                    break;

                default:
                    _logger.LogWarning("Cron job #{Id}: unknown action '{Action}' - skipped.", id, action);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cron job #{Id}: error executing action '{Action}'.", id, action);
        }
        finally
        {
            await UpdateLastRunAsync(id, context.NextFireTimeUtc, context.CancellationToken);
        }
    }

    private async Task UpdateLastRunAsync(int id, DateTimeOffset? nextFireTime, CancellationToken ct)
    {
        try
        {
            await _apiClient.UpdateCronJobRunAsync(new UpdateCronJobRunRequest
            {
                IdCronjob = id,
                LastRunAt = DateTime.UtcNow.ToString("O"),
                NextRunAt = nextFireTime?.UtcDateTime.ToString("O") ?? string.Empty,
            }, cancellationToken: ct);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Failed to update last_run_at for cron job #{Id}.", id);
        }
    }
}
