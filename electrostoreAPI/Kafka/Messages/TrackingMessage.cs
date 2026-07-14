namespace ElectrostoreAPI.Kafka.Messages;

// ---- Outgoing messages (produced by the API, consumed by the CRON) ----

public record TrackingActionMessage
{
    public string tracking_number { get; init; } = string.Empty;
    public int?   carrier         { get; init; }   // 17track carrier code (register, changecarrier, stoptrack, retrack, deletetrack, push)
    public int?   carrier_old     { get; init; }   // old 17track carrier code (changecarrier only)
    public string? tag            { get; init; }   // changeinfo
    public string? note           { get; init; }   // changeinfo
    public bool?  auto_detect     { get; init; }   // register, changeinfo
}