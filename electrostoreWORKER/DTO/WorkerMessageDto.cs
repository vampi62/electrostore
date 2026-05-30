namespace ElectrostoreWORKER.DTO;

public record MqttEventMessage
{
    public string topic { get; init; } = string.Empty;
    public string payload { get; init; } = string.Empty;
}

public record IaStatusMessage
{
    public string action { get; init; } = string.Empty;       // training_started | training_completed | training_failed
    public int id_ia { get; init; }
    public string status { get; init; } = string.Empty;
    public string message { get; init; } = string.Empty;
    public int requested_by { get; init; }
    public float accuracy { get; init; }
    public float val_accuracy { get; init; }
    public float loss { get; init; }
    public float val_loss { get; init; }
    public int epoch { get; init; }
}
