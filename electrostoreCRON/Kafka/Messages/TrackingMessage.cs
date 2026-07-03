namespace ElectrostoreCRON.Kafka.Messages;

// ---- Incoming messages (produced by the API, consumed by the CRON) ----

public record TrackingActionMessage
{
    public int    id_command      { get; init; }
    public string tracking_number { get; init; } = string.Empty;
    public int?   carrier         { get; init; }   // 17track carrier code (register, changecarrier, stoptrack, retrack, deletetrack, push)
    public int?   carrier_old     { get; init; }   // old 17track carrier code (changecarrier only)
    public string? tag            { get; init; }   // changeinfo
    public string? note           { get; init; }   // changeinfo
    public bool?  auto_detect     { get; init; }   // register, changeinfo
}

// ---- Outgoing messages (produced by the CRON, consumed by the API) ----

public record TrackingResultMessage
{
    public string action          { get; init; } = string.Empty;  // register|changecarrier|changeinfo|stoptrack|retrack|deletetrack|push
    public int    id_command      { get; init; }
    public string tracking_number { get; init; } = string.Empty;
    public int    carrier         { get; init; }
    public int?   carrier_old     { get; init; }
    public bool   success         { get; init; }
    public int?   error_code      { get; init; }
}
