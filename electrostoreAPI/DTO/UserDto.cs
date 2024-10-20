using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadUserDto
{
    public int id_user { get; init; }
    public string nom_user { get; init; }
    public string prenom_user { get; init; }
    public string email_user { get; init; }
    public string role_user { get; init; }
}
public record CreateUserDto
{
    [Required] public string nom_user { get; init; }
    [Required] public string prenom_user { get; init; }
    [Required] public string email_user { get; init; }
    [Required] public string mdp_user { get; init; }
    [Required] public string role_user { get; init; }
}
public record UpdateUserDto
{
    public string? nom_user { get; init; }
    public string? prenom_user { get; init; }
    public string? email_user { get; init; }
    public string? mdp_user { get; init; }
    public string? role_user { get; init; }
}
public record ReadConfig
{
    public bool smtp_enabled { get; init; }
    public bool mqtt_connected { get; init; }
    public bool ia_connected { get; init; }
}