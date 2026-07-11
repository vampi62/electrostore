namespace ElectrostoreCRON.Kafka.Messages;

// ---- Événements Kafka "cronjob-events" ----

public record CronJobEvent
{
    public required string action { get; init; }   // created | updated | deleted
    public CronJobEventData? data { get; init; }
}

public record CronJobEventData
{
    public int id_cronjob { get; init; }
    public string? name_cronjob { get; init; }
    public string? cron_expression { get; init; }
    public int? action_cronjob { get; init; }
    public string? params_cronjob { get; init; }
    public bool is_enabled { get; init; }
}
