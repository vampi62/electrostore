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
                CronExpression = c.cron_expression,
                ActionCronjob = c.action_cronjob,
                ParamsCronjob = c.params_cronjob ?? string.Empty,
                LastRunAt = c.last_run_at?.ToString("o") ?? string.Empty,
                NextRunAt = c.next_run_at?.ToString("o") ?? string.Empty
        }));
        return reply;
    }

    public override async Task<UpdateCronJobRunReply> UpdateCronJobRun(
        UpdateCronJobRunRequest request, ServerCallContext context)
    {
        await _cronJobService.UpdateCronJobRunAsync(
            request.IdCronjob,
            string.IsNullOrWhiteSpace(request.LastRunAt) ? null : DateTime.Parse(request.LastRunAt, null, System.Globalization.DateTimeStyles.RoundtripKind),
            string.IsNullOrWhiteSpace(request.NextRunAt) ? null : DateTime.Parse(request.NextRunAt, null, System.Globalization.DateTimeStyles.RoundtripKind),
            context.CancellationToken);
        return new UpdateCronJobRunReply { Success = true };
    }
}
