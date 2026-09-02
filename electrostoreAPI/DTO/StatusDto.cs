namespace ElectrostoreAPI.Dto;

public record ReadStatusDto
{
    public string api_status { get; init; } = "healthy";
    public bool db_connected { get; init; }
    public bool mqtt_connected { get; init; }
    public bool kafka_connected { get; init; }
    public required string ai_status { get; init; }
    public int ai_training_in_progress { get; init; }
    public required string notif_status { get; init; }
    public bool? notif_smtp { get; init; }
    public bool? notif_web_push { get; init; }
    public required string cron_status { get; init; }
    public required string worker_status { get; init; }
    public Dictionary<string, string> external_services { get; init; } = new();
}
