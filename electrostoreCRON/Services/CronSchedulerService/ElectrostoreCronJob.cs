using System.Text.Json;
using ElectrostoreCRON.DTO;
using ElectrostoreCRON.Grpc;
using ElectrostoreCRON.Services.ParcelTrackerService;
using Grpc.Core;
using Quartz;

namespace ElectrostoreCRON.Services.CronSchedulerService;

[DisallowConcurrentExecution]
public class ElectrostoreCronJob : IJob
{
    public const string KeyAction   = "action_cronjob";
    public const string KeyParams   = "params_cronjob";
    public const string KeyId       = "id_cronjob";

    private readonly IParcelTrackerService _parcelTracker;
    private readonly CRONToAPIGrpc.CRONToAPIGrpcClient _apiClient;
    private readonly ILogger<ElectrostoreCronJob> _logger;

    public ElectrostoreCronJob(
        IParcelTrackerService parcelTracker,
        CRONToAPIGrpc.CRONToAPIGrpcClient apiClient,
        ILogger<ElectrostoreCronJob> logger)
    {
        _parcelTracker = parcelTracker;
        _apiClient     = apiClient;
        _logger        = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var map        = context.JobDetail.JobDataMap;
        var action     = map.GetString(KeyAction) ?? string.Empty;
        var paramsJson = map.GetString(KeyParams);
        var id         = map.GetInt(KeyId);

        _logger.LogInformation("Running cron job #{Id} — action={Action}", id, action);

        try
        {
            switch (action)
            {
                case "track_parcel":
                    await _parcelTracker.TrackAsync(paramsJson, context.CancellationToken);
                    break;

                default:
                    _logger.LogWarning("Cron job #{Id}: unknown action '{Action}' — skipped.", id, action);
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
