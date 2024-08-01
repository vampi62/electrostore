using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadIADto
{
    public int id_ia { get; init; }
    public string nom_ia { get; init; }
    public string description_ia { get; init; }
    public DateTime date_ia { get; init; }
}
public record CreateIADto
{
    [Required] public string nom_ia { get; init; }
    [Required] public string description_ia { get; init; }
}
public record UpdateIADto
{
    public string? nom_ia { get; init; }
    public string? description_ia { get; init; }
}