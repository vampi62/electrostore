using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadProjetProjetTagDto
{
    public int id_projet { get; init; }
    public int id_projet_tag { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedProjetProjetTagDto : ReadProjetProjetTagDto
{
    public ReadProjetTagDto? projet_tag { get; init; }
    public ReadProjetDto? projet { get; init; }
}
public record ReadBulkProjetProjetTagDto
{
    public required List<ReadProjetProjetTagDto> Valide { get; init; }
    public required List<ErrorDetail> Error { get; init; }
}
public record CreateProjetProjetTagByProjetTagDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_projet { get; init; }
}
public record CreateProjetProjetTagByProjetDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_projet_tag { get; init; }
}
public record CreateProjetProjetTagDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_projet { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    public required int id_projet_tag { get; init; }
}