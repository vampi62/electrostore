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
    [MaxLength(2048, ErrorMessage = "{0} cannot exceed {1} characters")]
    public required string endpoint { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(512, ErrorMessage = "{0} cannot exceed {1} characters")]
    public required string p256dh { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(256, ErrorMessage = "{0} cannot exceed {1} characters")]
    public required string auth { get; init; }

    [MaxLength(255, ErrorMessage = "{0} cannot exceed {1} characters")]
    public string? device_name { get; init; }
}
public record CreateUserPushSubscriptionDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(2048, ErrorMessage = "{0} cannot exceed {1} characters")]
    public required string endpoint { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(512, ErrorMessage = "{0} cannot exceed {1} characters")]
    public required string p256dh { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(256, ErrorMessage = "{0} cannot exceed {1} characters")]
    public required string auth { get; init; }

    [MaxLength(255, ErrorMessage = "{0} cannot exceed {1} characters")]
    public string? device_name { get; init; }
    public int id_user { get; init; }
}
