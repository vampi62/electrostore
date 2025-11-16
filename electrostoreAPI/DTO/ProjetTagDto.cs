using System.ComponentModel.DataAnnotations;
using electrostore.Validators;

namespace electrostore.Dto;

public record ReadProjetTagDto
{
    public int id_projet_tag { get; init; }
    public required string nom_projet_tag { get; init; }
    public int poids_projet_tag { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedProjetTagDto : ReadProjetTagDto
{
    public int projets_projet_tags_count { get; init; }
    public IEnumerable<ReadProjetProjetTagDto>? projets_projet_tags { get; init; }
}

public record ReadBulkProjetTagDto
{
    public required List<ReadProjetTagDto> Valide { get; init; }
    public required List<ErrorDetail> Error { get; init; }
}

public record CreateProjetTagDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? nom_projet_tag { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? poids_projet_tag { get; init; }
}
public record UpdateProjetTagDto
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? nom_projet_tag { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? poids_projet_tag { get; init; }
}