using System.Text.Json;
using Confluent.Kafka;
using ElectrostoreCRON.Kafka.Messages;
using ElectrostoreCRON.Services.CronSchedulerService;
using Quartz;
using Quartz.Impl.Matchers;

namespace ElectrostoreCRON.Kafka.Consumers;

public class KafkaCronJobEventsConsumer : BackgroundService
{
    private const string Topic = "cronjob-events";

    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaCronJobEventsConsumer> _logger;

    public KafkaCronJobEventsConsumer(
        ISchedulerFactory schedulerFactory,
        IConfiguration configuration,
        ILogger<KafkaCronJobEventsConsumer> logger)
    {
        _schedulerFactory = schedulerFactory;
        _configuration    = configuration;
        _logger           = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrapServers = _configuration.GetSection("Kafka:BootstrapServers").Value ?? "kafka:9092";
        var groupId = _configuration.GetSection("Kafka:ConsumerGroupId").Value ?? "cron-service-events";

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId          = groupId,
            AutoOffsetReset  = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(Topic);

        _logger.LogInformation(
            "KafkaCronJobEventsConsumer started (group={Group}, servers={Servers})", groupId, bootstrapServers);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = await ConsumeMessageAsync(consumer, stoppingToken);
                if (result is null)
                {
                    continue;
                }
                await ProcessMessageAsync(consumer, result, stoppingToken);
            }
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("KafkaCronJobEventsConsumer stopped");
        }
    }

    private async Task<ConsumeResult<string, string>?> ConsumeMessageAsync(
        IConsumer<string, string> consumer, 
        CancellationToken ct)
    {
        try
        {
            return await Task.Run(() => consumer.Consume(ct), ct);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (ConsumeException ex)
        {
            _logger.LogError(ex, "Kafka error: {Reason}", ex.Error.Reason);
            return null;
        }
    }

    private async Task ProcessMessageAsync(
        IConsumer<string, string> consumer,
        ConsumeResult<string, string> result,
        CancellationToken ct)
    {
        if (result.IsPartitionEOF || result.Message?.Value is null)
        {
            return;
        }
        var msg = DeserializeMessage(result);
        if (msg is null)
        {
            consumer.Commit(result);
            return;
        }
        var dispatched = await HandleEventAsync(msg, ct);
        if (dispatched)
        {
            consumer.Commit(result);
        } else
        {
            _logger.LogWarning("Message for offset {Offset} was not dispatched.", result.Offset);
        }
    }

    private CronJobEvent? DeserializeMessage(ConsumeResult<string, string> result)
    {
        try
        {
            return JsonSerializer.Deserialize<CronJobEvent>(result.Message.Value, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid Kafka message (JSON) - offset {Offset}", result.Offset);
            return null;
        }
    }

    private async Task<bool> HandleEventAsync(CronJobEvent evt, CancellationToken ct)
    {
        if (evt is null)
        {
            return false;
        }
        _logger.LogInformation("CronJob event received: action={Action}, id={Id}", evt.action, evt.data?.id_cronjob);
        var scheduler = await _schedulerFactory.GetScheduler(ct);
        switch (evt.action)
        {
            case "created":
                if (evt.data is not null && evt.data.is_enabled)
                {
                    await ScheduleOrReplaceJobAsync(scheduler, evt.data, ct);
                }
                break;
            case "updated":
                if (evt.data is not null)
                {
                    await RemoveJobAsync(scheduler, evt.data.id_cronjob, ct);
                    if (evt.data.is_enabled)
                    {
                        await ScheduleOrReplaceJobAsync(scheduler, evt.data, ct);
                    }
                }
                break;
            case "deleted":
                if (evt.data is not null)
                {
                    await RemoveJobAsync(scheduler, evt.data.id_cronjob, ct);
                }
                break;
            default:
                _logger.LogWarning("Unknown cronjob-event action: {Action}", evt.action);
                break;
        }
        return true;
    }

    private async Task ScheduleOrReplaceJobAsync(IScheduler scheduler, CronJobEventData job, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(job.cron_expression))
        {
            _logger.LogWarning("CronJob #{Id}: empty cron expression, skipped.", job.id_cronjob);
            return;
        }

        var jobKey = new JobKey($"job-{job.id_cronjob}", "electrostore");

        // Supprimer l'ancienne version si elle existe
        await RemoveJobAsync(scheduler, job.id_cronjob, ct);

        var jobDetail = JobBuilder.Create<ElectrostoreCronJob>()
            .WithIdentity(jobKey)
            .UsingJobData(ElectrostoreCronJob.KeyId,     job.id_cronjob)
            .UsingJobData(ElectrostoreCronJob.KeyAction,  (int)job.action_cronjob!)
            .UsingJobData(ElectrostoreCronJob.KeyParams,  job.params_cronjob ?? string.Empty)
            .Build();

        // Normalize 5-field Unix cron (m h dom mon dow) to 6-field Quartz cron (s m h dom mon dow)
        var cronExpression = job.cron_expression.Trim();
        if (cronExpression.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length == 5)
        {
            cronExpression = "0 " + cronExpression;
        }

        ITrigger trigger;
        try
        {
            trigger = TriggerBuilder.Create()
                .WithIdentity($"trigger-{job.id_cronjob}", "electrostore")
                .WithCronSchedule(cronExpression)
                .Build();
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex,
                "CronJob #{Id}: invalid cron expression '{Expr}' - skipped.", job.id_cronjob, job.cron_expression);
            return;
        }

        await scheduler.ScheduleJob(jobDetail, trigger, ct);
        _logger.LogInformation(
            "CronJob #{Id} scheduled via event - action={Action}, expr={Expr}",
            job.id_cronjob, job.action_cronjob, job.cron_expression);
    }

    private async Task RemoveJobAsync(IScheduler scheduler, int idCronjob, CancellationToken ct)
    {
        var jobKey = new JobKey($"job-{idCronjob}", "electrostore");
        if (await scheduler.CheckExists(jobKey, ct))
        {
            await scheduler.DeleteJob(jobKey, ct);
            _logger.LogInformation("CronJob #{Id} removed from scheduler.", idCronjob);
        }
    }
}
