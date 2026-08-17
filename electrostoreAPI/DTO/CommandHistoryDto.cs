using ElectrostoreAPI.Enums;

namespace ElectrostoreAPI.Dto;

public record ReadCommandHistoryDto
{
    public int id_command_history { get; init; }
    public int id_command { get; init; }
    public TrackingStatus? status_command_history { get; init; }
    public TrackingSubStatus? sub_status_command_history { get; init; }
    public string? description_command_history { get; init; }
    public string? location_command_history { get; init; }
    public string? stage_command_history { get; init; }
    public DateTime? event_time_utc { get; init; }
    public string? timezone_command_history { get; init; }
    public string? country_command_history { get; init; }
    public string? state_command_history { get; init; }
    public string? city_command_history { get; init; }
    public string? postal_code_command_history { get; init; }
    public string? latitude_command_history { get; init; }
    public string? longitude_command_history { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}

public record CreateCommandHistoryDto
{
    public int id_command { get; init; }
    public TrackingStatus? status_command_history { get; init; }
    public TrackingSubStatus? sub_status_command_history { get; init; }
    public string? description_command_history { get; init; }
    public string? location_command_history { get; init; }
    public string? stage_command_history { get; init; }
    public DateTime? event_time_utc { get; init; }
    public string? timezone_command_history { get; init; }
    public string? country_command_history { get; init; }
    public string? state_command_history { get; init; }
    public string? city_command_history { get; init; }
    public string? postal_code_command_history { get; init; }
    public string? latitude_command_history { get; init; }
    public string? longitude_command_history { get; init; }
}