namespace ElectrostoreWORKER.Kafka.Messages;

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
