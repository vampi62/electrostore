using System.ComponentModel.DataAnnotations;

namespace ElectrostoreAPI.Dto;

public record ReadUserPushSubscriptionDto
{
    public int id_push_subscription { get; init; }
    public int id_user { get; init; }
    public required string endpoint { get; init; }
    public required string p256dh { get; init; }
    public required string auth { get; init; }
    public string? device_name { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record CreateUserPushSubscriptionDtoByUserId
{
    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxUrlLength, ErrorMessage = "{0} cannot exceed {1} characters")]
    public required string endpoint { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxPushKeyLength, ErrorMessage = "{0} cannot exceed {1} characters")]
    public required string p256dh { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxPushAuthLength, ErrorMessage = "{0} cannot exceed {1} characters")]
    public required string auth { get; init; }

    [MaxLength(Constants.MaxDeviceNameLength, ErrorMessage = "{0} cannot exceed {1} characters")]
    public string? device_name { get; init; }
}
public record CreateUserPushSubscriptionDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxUrlLength, ErrorMessage = "{0} cannot exceed {1} characters")]
    public required string endpoint { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxPushKeyLength, ErrorMessage = "{0} cannot exceed {1} characters")]
    public required string p256dh { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxPushAuthLength, ErrorMessage = "{0} cannot exceed {1} characters")]
    public required string auth { get; init; }

    [MaxLength(Constants.MaxDeviceNameLength, ErrorMessage = "{0} cannot exceed {1} characters")]
    public string? device_name { get; init; }
    public int id_user { get; init; }
}
