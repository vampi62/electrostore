using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace ElectrostoreAPI.Grpc.Services;

public class CronJobsGrpcService : CronJobsGrpc.CronJobsGrpcBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CronJobsGrpcService> _logger;

    public CronJobsGrpcService(
        ApplicationDbContext context,
        ILogger<CronJobsGrpcService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public override async Task<GetEnabledCronJobsReply> GetEnabledCronJobs(
        GetEnabledCronJobsRequest request, ServerCallContext context)
    {
        var jobs = await _context.CronJobs
            .Where(c => c.is_enabled)
            .Select(c => new CronJobItem
            {
                IdCronjob = c.id_cronjob,
                NameCronjob = c.name_cronjob,
                CronExpression = c.cron_expression,
                ActionCronjob = c.action_cronjob,
                ParamsCronjob = c.params_cronjob ?? string.Empty,
            })
            .ToListAsync(context.CancellationToken);

        var reply = new GetEnabledCronJobsReply();
        reply.CronJobs.AddRange(jobs);
        return reply;
    }

    public override async Task<UpdateCronJobRunReply> UpdateCronJobRun(
        UpdateCronJobRunRequest request, ServerCallContext context)
    {
        var job = await _context.CronJobs.FindAsync(
            new object[] { request.IdCronjob }, context.CancellationToken);

        if (job is null)
        {
            _logger.LogWarning("UpdateCronJobRun : cronjob #{Id} introuvable.", request.IdCronjob);
            return new UpdateCronJobRunReply { Success = false };
        }

        if (!string.IsNullOrWhiteSpace(request.LastRunAt)
            && DateTime.TryParse(request.LastRunAt, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var lastRun))
        {
            job.last_run_at = lastRun;
        }

        if (!string.IsNullOrWhiteSpace(request.NextRunAt)
            && DateTime.TryParse(request.NextRunAt, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var nextRun))
        {
            job.next_run_at = nextRun;
        }

        await _context.SaveChangesAsync(context.CancellationToken);
        return new UpdateCronJobRunReply { Success = true };
    }
}
