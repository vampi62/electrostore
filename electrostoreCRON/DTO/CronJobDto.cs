namespace ElectrostoreCRON.DTO;

public record ParcelTrackingMessage
{
    public string action { get; init; } = "track_parcel";
    public int id_command { get; init; }
    public string tracking_number { get; init; } = string.Empty;
    public string carrier { get; init; } = string.Empty;
    public string tracking_status { get; init; } = string.Empty;
    public string tracking_event { get; init; } = string.Empty;
    public bool is_delivered { get; init; }
    public string date_livraison { get; init; } = string.Empty;
}

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
    public string? action_cronjob { get; init; }
    public string? params_cronjob { get; init; }
    public bool is_enabled { get; init; }
}

// ---- Paramètres dans params_cronjob (action = track_parcel) ----

public record ParcelTrackingParams
{
    public int id_command { get; init; }
    public string tracking_number { get; init; } = string.Empty;
    public string carrier { get; init; } = string.Empty;  // colissimo | dhl | ups | 17track | ...
}

