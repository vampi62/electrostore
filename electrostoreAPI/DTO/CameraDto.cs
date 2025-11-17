using System.ComponentModel.DataAnnotations;
using electrostore.Validators;

namespace electrostore.Dto;

public record ReadCameraDto
{
    public int id_camera { get; init; } 
    public required string nom_camera { get; init; }
    public required string url_camera { get; init; }
    public string? user_camera { get; init; }
    public string? mdp_camera { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record CreateCameraDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string nom_camera { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxUrlLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string url_camera { get; init; }

    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? user_camera { get; init; }

    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? mdp_camera { get; init; }
}
public record UpdateCameraDto
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? nom_camera { get; init; }

    [MaxLength(Constants.MaxUrlLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? url_camera { get; init; }

    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? user_camera { get; init; }

    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? mdp_camera { get; init; }
}
public record CameraLightDto
{
    public required bool state { get; init; }
}
public record CameraStatusDto
{
    public bool network { get; init; }
    public int statusCode { get; init; }
    public float? uptime { get; init; }
    public string? espModel { get; init; }
    public float? espTemperature { get; init; }
    public string? OTAWait { get; init; }
    public string? OTAUploading { get; init; }
    public string? OTAError { get; init; }
    public int? OTATime { get; init; }
    public int? OTARemainingTime { get; init; }
    public float? OTAPercentage { get; init; }
    public int? ringLightPower { get; init; }
    public string? versionScanBox { get; init; }
    public string? cameraResolution { get; init; }
    public string? cameraPID { get; init; }
    public string? wifiSignalStrength { get; init; }
}