namespace ElectrostoreWORKER.DTO;

public record ParcelTrackingMessage
{
    public string action { get; init; } = string.Empty;
    public int id_command { get; init; }
    public string tracking_number { get; init; } = string.Empty;
    public string carrier { get; init; } = string.Empty;
    public string tracking_status { get; init; } = string.Empty;
    public string tracking_event { get; init; } = string.Empty;
    public bool is_delivered { get; init; }
    public string date_livraison { get; init; } = string.Empty;
}

public record PriceUpdateMessage
{
    public string action { get; init; } = string.Empty;
    public int id_command { get; init; }
    public int id_item { get; init; }
    public float price { get; init; }
    public string provider { get; init; } = string.Empty;
}

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
