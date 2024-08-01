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