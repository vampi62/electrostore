using Grpc.Core;
using ElectrostoreAPI.Services.CronJobService;

namespace ElectrostoreAPI.Grpc.Services;

public class CronJobsGrpcService : CronJobsGrpc.CronJobsGrpcBase
{
    private readonly ICronJobService _cronJobService;
    private readonly ILogger<CronJobsGrpcService> _logger;

    public CronJobsGrpcService(
        ICronJobService cronJobService,
        ILogger<CronJobsGrpcService> logger)
    {
        _cronJobService = cronJobService;
        _logger = logger;
    }

    public override async Task<GetEnabledCronJobsReply> GetEnabledCronJobs(
        GetEnabledCronJobsRequest request, ServerCallContext context)
    {
        var jobs = await _cronJobService.GetEnabledCronJobsAsync(context.CancellationToken);
        var reply = new GetEnabledCronJobsReply();
        reply.CronJobs.AddRange(jobs.Select(c => new CronJobItem
        {
                IdCronjob = c.id_cronjob,
                NameCronjob = c.name_cronjob,
                CronExpressionCronjob = c.cron_expression_cronjob,
                ActionCronjob = (CronJobAction)(int)c.action_cronjob,
                ParamsCronjob = c.params_cronjob ?? string.Empty,
                LastRunAt = c.last_run_at?.ToString("o") ?? string.Empty,
                NextRunAt = c.next_run_at?.ToString("o") ?? string.Empty
        }));
        _logger.LogDebug("GetEnabledCronJobs: returned {Count} job(s)", reply.CronJobs.Count);
        return reply;
    }

    public override async Task<UpdateCronJobRunReply> UpdateCronJobRun(
        UpdateCronJobRunRequest request, ServerCallContext context)
    {
        try
        {
            await _cronJobService.UpdateCronJobRunAsync(
                request.IdCronjob,
                string.IsNullOrWhiteSpace(request.LastRunAt) ? null : DateTime.Parse(request.LastRunAt, null, System.Globalization.DateTimeStyles.RoundtripKind),
                string.IsNullOrWhiteSpace(request.NextRunAt) ? null : DateTime.Parse(request.NextRunAt, null, System.Globalization.DateTimeStyles.RoundtripKind),
                context.CancellationToken);
            _logger.LogInformation("UpdateCronJobRun: cronjob={Id} lastRunAt={LastRunAt} nextRunAt={NextRunAt}",
                request.IdCronjob, request.LastRunAt, request.NextRunAt);
            return new UpdateCronJobRunReply { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateCronJobRun: error for cronjob={Id}", request.IdCronjob);
            return new UpdateCronJobRunReply { Success = false };
        }
    }

    public override async Task<UpdateCronJobStatusReply> UpdateCronJobStatus(
        UpdateCronJobStatusRequest request, ServerCallContext context)
    {
        try
        {
            await _cronJobService.UpdateCronJobStatusAsync(
                request.IdCronjob,
                (Enums.CronJobStatus)(int)request.StatusCronjob,
                string.IsNullOrWhiteSpace(request.LastErrorCronjob) ? null : request.LastErrorCronjob,
                context.CancellationToken);
            _logger.LogInformation("UpdateCronJobStatus: cronjob={Id} status={Status}",
                request.IdCronjob, request.StatusCronjob);
            return new UpdateCronJobStatusReply { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateCronJobStatus: error for cronjob={Id}", request.IdCronjob);
            return new UpdateCronJobStatusReply { Success = false };
        }
    }
}
