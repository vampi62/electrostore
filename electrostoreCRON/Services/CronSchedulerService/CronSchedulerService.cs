using ElectrostoreCRON.Grpc;
using Grpc.Core;
using Quartz;
using Quartz.Impl.Matchers;

namespace ElectrostoreCRON.Services.CronSchedulerService;

public class CronSchedulerService : BackgroundService
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly CronJobsGrpc.CronJobsGrpcClient _apiClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CronSchedulerService> _logger;

    public CronSchedulerService(
        ISchedulerFactory schedulerFactory,
        CronJobsGrpc.CronJobsGrpcClient apiClient,
        IConfiguration configuration,
        ILogger<CronSchedulerService> logger)
    {
        _schedulerFactory = schedulerFactory;
        _apiClient        = apiClient;
        _configuration    = configuration;
        _logger           = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var scheduler = await _schedulerFactory.GetScheduler(stoppingToken);
        await scheduler.Start(stoppingToken);

        await LoadAndScheduleJobsAsync(scheduler, stoppingToken);

        var refreshMinutes = _configuration.GetValue<int>("CronRefreshIntervalMinutes", 60);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(refreshMinutes));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            _logger.LogInformation("Refreshing cron job schedule...");
            await scheduler.UnscheduleJobs(
                (await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup())).ToList(),
                stoppingToken);
            await LoadAndScheduleJobsAsync(scheduler, stoppingToken);
        }
    }

    private async Task LoadAndScheduleJobsAsync(IScheduler scheduler, CancellationToken ct)
    {
        GetEnabledCronJobsReply reply;
        try
        {
            reply = await _apiClient.GetEnabledCronJobsAsync(new GetEnabledCronJobsRequest(), cancellationToken: ct);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Failed to retrieve cron jobs from API.");
            return;
        }

        _logger.LogInformation("{Count} cron job(s) loaded from API.", reply.CronJobs.Count);

        foreach (var job in reply.CronJobs)
        {
            if (string.IsNullOrWhiteSpace(job.CronExpression))
            {
                _logger.LogWarning("Cron job #{Id} ({Name}): empty cron expression, skipped.", job.IdCronjob, job.NameCronjob);
                continue;
            }

            var jobKey = new JobKey($"job-{job.IdCronjob}", "electrostore");

            var jobDetail = JobBuilder.Create<ElectrostoreCronJob>()
                .WithIdentity(jobKey)
                .UsingJobData(ElectrostoreCronJob.KeyId,     job.IdCronjob)
                .UsingJobData(ElectrostoreCronJob.KeyAction,  (int)job.ActionCronjob)
                .UsingJobData(ElectrostoreCronJob.KeyParams,  job.ParamsCronjob ?? string.Empty)
                .Build();

            // Normalize 5-field Unix cron (m h dom mon dow) to 6-field Quartz cron (s m h dom mon dow)
            var cronExpression = job.CronExpression.Trim();
            if (cronExpression.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length == 5)
            {
                cronExpression = "0 " + cronExpression;
            }

            ITrigger trigger;
            try
            {
                trigger = TriggerBuilder.Create()
                    .WithIdentity($"trigger-{job.IdCronjob}", "electrostore")
                    .WithCronSchedule(cronExpression)
                    .Build();
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex,
                    "Cron job #{Id} ({Name}): invalid cron expression '{Expr}' - skipped.",
                    job.IdCronjob, job.NameCronjob, cronExpression);
                continue;
            }

            await scheduler.ScheduleJob(jobDetail, trigger, ct);
            _logger.LogInformation(
                "Cron job #{Id} ({Name}) scheduled - action={Action}, expr={Expr}",
                job.IdCronjob, job.NameCronjob, job.ActionCronjob, job.CronExpression);
        }
    }
}
