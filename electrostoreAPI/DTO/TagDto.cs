using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadTagDto
{
    public int id_tag { get; init; }
    public string nom_tag { get; init; }
    public int poids_tag { get; init; }
}
public record CreateTagDto
{
    [Required] public string nom_tag { get; init; }
    [Required] public int poids_tag { get; init; }
}
public record UpdateTagDto
{
    public string? nom_tag { get; init; }
    public int? poids_tag { get; init; }
}