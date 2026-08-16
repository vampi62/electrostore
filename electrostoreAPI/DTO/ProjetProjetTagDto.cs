using System.ComponentModel.DataAnnotations;

namespace ElectrostoreAPI.Dto;

public record ReadProjetProjetTagDto
{
    public int id_project { get; init; }
    public int id_project_tag { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedProjetProjetTagDto : ReadProjetProjetTagDto
{
    public ReadProjetTagDto? project_tag { get; init; }
    public ReadProjetDto? project { get; init; }
}
public record ReadBulkProjetProjetTagDto
{
    public required List<ReadProjetProjetTagDto> Valide { get; init; }
    public required List<ErrorDetail> Error { get; init; }
}
public record CreateProjetProjetTagByProjetTagDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_project { get; init; }
}
public record CreateProjetProjetTagByProjetDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_project_tag { get; init; }
}
public record CreateProjetProjetTagDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_project { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    public required int id_project_tag { get; init; }
}