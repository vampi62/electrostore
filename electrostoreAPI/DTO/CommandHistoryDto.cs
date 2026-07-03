using ElectrostoreAPI.Enums;

namespace ElectrostoreAPI.Dto;

public record ReadCommandHistoryDto
{
    public int id_command_history { get; init; }
    public int id_command { get; init; }
    public TrackingStatus? status { get; init; }
    public string? sub_status { get; init; }
    public string? description { get; init; }
    public string? location { get; init; }
    public string? stage { get; init; }
    public DateTime? event_time_utc { get; init; }
    public string? timezone { get; init; }
    public string? country { get; init; }
    public string? state { get; init; }
    public string? city { get; init; }
    public string? postal_code { get; init; }
    public string? latitude { get; init; }
    public string? longitude { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}

public record CreateCommandHistoryDto
{
    public int id_command { get; init; }
    public TrackingStatus? status { get; init; }
    public string? sub_status { get; init; }
    public string? description { get; init; }
    public string? location { get; init; }
    public string? stage { get; init; }
    public DateTime? event_time_utc { get; init; }
    public string? timezone { get; init; }
    public string? country { get; init; }
    public string? state { get; init; }
    public string? city { get; init; }
    public string? postal_code { get; init; }
    public string? latitude { get; init; }
    public string? longitude { get; init; }
}