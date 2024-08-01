using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadProjetDto
{
    public int id_projet { get; init; }
    public string nom_projet { get; init; }
    public string description_projet { get; init; }
    public string url_projet { get; init; }
    public string status_projet { get; init; }
    public DateTime date_projet { get; init; }
    public DateTime? date_fin_projet { get; init; }
}
public record CreateProjetDto
{
    [Required] public string nom_projet { get; init; }
    [Required] public string description_projet { get; init; }
    [Required] public string url_projet { get; init; }
    [Required] public string status_projet { get; init; }
    [Required] public DateTime date_projet { get; init; }
    public DateTime? date_fin_projet { get; init; }
}
public record UpdateProjetDto
{
    public string? nom_projet { get; init; }
    public string? description_projet { get; init; }
    public string? url_projet { get; init; }
    public string? status_projet { get; init; }
    public DateTime? date_projet { get; init; }
    public DateTime? date_fin_projet { get; init; }
}