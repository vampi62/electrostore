using System.ComponentModel.DataAnnotations;

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
    [Required]
    [MinLength(1, ErrorMessage = "nom_projet_tag cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "nom_projet_tag cannot exceed 50 characters")]
    public required string nom_projet_tag { get; init; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "poids_projet_tag must be greater than or equal to 0.")]
    public required int? poids_projet_tag { get; init; } = 0;
}
public record UpdateProjetTagDto : IValidatableObject
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "nom_projet_tag cannot exceed 50 characters")]
    public string? nom_projet_tag { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "poids_projet_tag must be greater than or equal to 0.")]
    public int? poids_projet_tag { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (nom_projet_tag is not null && string.IsNullOrWhiteSpace(nom_projet_tag))
        {
            yield return new ValidationResult("nom_projet_tag cannot be empty or whitespace.", new[] { nameof(nom_projet_tag) });
        }
    }
}