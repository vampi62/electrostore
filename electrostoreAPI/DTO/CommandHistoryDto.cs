namespace ElectrostoreAPI.Dto;

public record ReadCommandHistoryDto
{
    public int id_command_history { get; init; }
    public int id_command { get; init; }
    public required string status_command_history { get; init; }
    public string? tracking_number { get; init; }
    public string? carrier { get; init; }
    public string? tracking_event { get; init; }
    public DateTime event_at { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
