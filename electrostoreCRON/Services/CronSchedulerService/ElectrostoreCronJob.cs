using ElectrostoreCRON.Grpc;

using ElectrostoreCRON.Services.CronJobExecutionRegistry;
using Grpc.Core;
using Quartz;

namespace ElectrostoreCRON.Services.CronSchedulerService;

[DisallowConcurrentExecution]
public class ElectrostoreCronJob : IJob
{
    public const string KeyAction = "action_cronjob";
    public const string KeyParams = "params_cronjob";
    public const string KeyId     = "id_cronjob";


    private readonly ICronJobExecutionRegistry     _executionRegistry;
    private readonly CronJobsGrpc.CronJobsGrpcClient _apiClient;
    private readonly ILogger<ElectrostoreCronJob>  _logger;

    public ElectrostoreCronJob(
        ICronJobExecutionRegistry executionRegistry,
        CronJobsGrpc.CronJobsGrpcClient apiClient,
        ILogger<ElectrostoreCronJob> logger)
    {
        _executionRegistry = executionRegistry;
        CronJobsGrpc.CronJobsGrpcClient apiClient,
        ILogger<ElectrostoreCronJob> logger)
    {
        _apiClient   = apiClient;
        _logger      = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var map    = context.JobDetail.JobDataMap;
        var action = Enum.TryParse<CronJobAction>(map.Get(KeyAction)?.ToString(), out var actionValue) ? (int)actionValue : -1;
        var id     = map.GetInt(KeyId);

        _logger.LogInformation("Running cron job #{Id} - action={Action}", id, action);

        var runToken = _executionRegistry.Register(id, context.CancellationToken);
        await UpdateStatusAsync(id, CronJobExecutionStatus.Running, null, context.CancellationToken);

        try
        {
            switch (action)
            {
                case (int)CronJobAction.PackageTracking:
                    // await _track17Sync.SyncAllAsync(context.CancellationToken);
                    break;

                default:
                    _logger.LogWarning("Cron job #{Id}: unknown action '{Action}' - skipped.", id, action);
                    break;
            }
            await UpdateStatusAsync(id, CronJobExecutionStatus.Success, null, context.CancellationToken);
        }
        catch (OperationCanceledException) when (runToken.IsCancellationRequested)
        {
            _logger.LogWarning("Cron job #{Id}: execution was force-stopped.", id);
            await UpdateStatusAsync(id, CronJobExecutionStatus.Stopped, null, context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cron job #{Id}: error executing action '{Action}'.", id, action);
            await UpdateStatusAsync(id, CronJobExecutionStatus.Failed, ex.Message, context.CancellationToken);
        }
        finally
        {
            _executionRegistry.Unregister(id);
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

    private async Task UpdateStatusAsync(int id, CronJobExecutionStatus status, string? lastError, CancellationToken ct)
    {
        try
        {
            await _apiClient.UpdateCronJobStatusAsync(new UpdateCronJobStatusRequest
            {
                IdCronjob = id,
                StatusCronjob = status,
                LastErrorCronjob = lastError ?? string.Empty,
            }, cancellationToken: ct);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Failed to update status for cron job #{Id}.", id);
        }
    }
}
