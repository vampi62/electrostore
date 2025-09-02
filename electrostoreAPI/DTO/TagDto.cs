using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadTagDto
{
    public int id_tag { get; init; }
    public required string nom_tag { get; init; }
    public int poids_tag { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedTagDto : ReadTagDto
{
    public int stores_tags_count { get; init; }
    public int items_tags_count { get; init; }
    public int boxs_tags_count { get; init; }
    public IEnumerable<ReadStoreTagDto>? stores_tags { get; init; }
    public IEnumerable<ReadItemTagDto>? items_tags { get; init; }
    public IEnumerable<ReadBoxTagDto>? boxs_tags { get; init; }
}

public record ReadBulkTagDto
{
    public required List<ReadTagDto> Valide { get; init; }
    public required List<ErrorDetail> Error { get; init; }
}

public record CreateTagDto
{
    [Required]
    [MinLength(1, ErrorMessage = "nom_tag cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "nom_tag cannot exceed 50 characters")]
    public required string nom_tag { get; init; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "poids_tag must be greater than or equal to 0.")]
    public required int? poids_tag { get; init; } = 0;
}
public record UpdateTagDto : IValidatableObject
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "nom_tag cannot exceed 50 characters")]
    public string? nom_tag { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "poids_tag must be greater than or equal to 0.")]
    public int? poids_tag { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (nom_tag is not null && string.IsNullOrWhiteSpace(nom_tag))
        {
            yield return new ValidationResult("nom_tag cannot be empty or whitespace.", new[] { nameof(nom_tag) });
        }
    }
}