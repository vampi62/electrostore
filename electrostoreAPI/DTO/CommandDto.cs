using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadCommandDto
{
    public int id_command { get; init; }
    public float prix_command { get; init; }
    public string url_command { get; init; }
    public string status_command { get; init; }
    public DateTime date_command { get; init; }
    public DateTime? date_livraison_command { get; init; }
}
public record CreateCommandDto
{
    [Required] public float prix_command { get; init; }
    [Required] public string url_command { get; init; }
    [Required] public string status_command { get; init; }
    [Required] public DateTime date_command { get; init; }
    public DateTime? date_livraison_command { get; init; }
}
public record UpdateCommandDto
{
    public float? prix_command { get; init; }
    public string? url_command { get; init; }
    public string? status_command { get; init; }
    public DateTime? date_command { get; init; }
    public DateTime? date_livraison_command { get; init; }
}