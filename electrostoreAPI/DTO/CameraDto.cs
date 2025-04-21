using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadCameraDto
{
    public int id_camera { get; init; } 
    public string nom_camera { get; init; }
    public string url_camera { get; init; }
    public string? user_camera { get; init; }
    public string? mdp_camera { get; init; }
}
public record CreateCameraDto : IValidatableObject
{
    [Required]
    [MinLength(1, ErrorMessage = "nom_camera cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "nom_camera cannot exceed 50 characters")]
    public string nom_camera { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "url_camera cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxUrlLength, ErrorMessage = "url_camera cannot exceed 150 characters")]
    public string url_camera { get; init; }

    [MinLength(1, ErrorMessage = "user_camera cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "user_camera cannot exceed 50 characters")]
    public string? user_camera { get; init; }

    [MinLength(1, ErrorMessage = "mdp_camera cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "mdp_camera cannot exceed 50 characters")]
    public string? mdp_camera { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(nom_camera))
        {
            yield return new ValidationResult("nom_camera cannot be empty or whitespace.", new[] { nameof(nom_camera) });
        }
        if (string.IsNullOrWhiteSpace(url_camera))
        {
            yield return new ValidationResult("url_camera cannot be empty or whitespace.", new[] { nameof(url_camera) });
        }
    }
}
public record UpdateCameraDto : IValidatableObject
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "nom_camera cannot exceed 50 characters")]
    public string? nom_camera { get; init; }

    [MaxLength(Constants.MaxUrlLength, ErrorMessage = "url_camera cannot exceed 150 characters")]
    public string? url_camera { get; init; }

    [MaxLength(Constants.MaxNameLength, ErrorMessage = "user_camera cannot exceed 50 characters")]
    public string? user_camera { get; init; }

    [MaxLength(Constants.MaxNameLength, ErrorMessage = "mdp_camera cannot exceed 50 characters")]
    public string? mdp_camera { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (nom_camera is not null && string.IsNullOrWhiteSpace(nom_camera))
        {
            yield return new ValidationResult("nom_camera cannot be empty or whitespace.", new[] { nameof(nom_camera) });
        }
        if (url_camera is not null && string.IsNullOrWhiteSpace(url_camera))
        {
            yield return new ValidationResult("url_camera cannot be empty or whitespace.", new[] { nameof(url_camera) });
        }
    }
}
public record CameraLightDto
{
    public bool state { get; init; }
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