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
public record CreateCameraDto
{
    [Required] public string nom_camera { get; init; }
    [Required] public string url_camera { get; init; }
    public string? user_camera { get; init; }
    public string? mdp_camera { get; init; }
}
public record UpdateCameraDto
{
    public string? nom_camera { get; init; }
    public string? url_camera { get; init; }
    public string? user_camera { get; init; }
    public string? mdp_camera { get; init; }
}
public record CameraLightDto
{
    public bool state { get; init; }
}
public record CameraStatusDto
{
    public float uptime { get; init; }
    public string espModel { get; init; }
    public float espTemperature { get; init; }
    public int ringLightPower { get; init; }
    public string versionScanBox { get; init; }
    public string cameraResolution { get; init; }
    public string cameraPID { get; init; }
    public string wifiSignalStrength { get; init; }
}