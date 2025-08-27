using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadProjetDto
{
    public int id_projet { get; init; }
    public string nom_projet { get; init; }
    public string description_projet { get; init; }
    public string url_projet { get; init; }
    public string status_projet { get; init; }
    public DateTime date_debut_projet { get; init; }
    public DateTime? date_fin_projet { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedProjetDto : ReadProjetDto
{
    public int projets_commentaires_count { get; init; }
    public int projets_documents_count { get; init; }
    public int projets_items_count { get; init; }
    public IEnumerable<ReadProjetCommentaireDto>? projets_commentaires { get; init; }
    public IEnumerable<ReadProjetDocumentDto>? projets_documents { get; init; }
    public IEnumerable<ReadProjetItemDto>? projets_items { get; init; }
}
public record CreateProjetDto : IValidatableObject
{
    [Required]
    [MinLength(1, ErrorMessage = "nom_projet cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "nom_projet cannot exceed 50 characters")]
    public required string nom_projet { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "description_projet cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "description_projet cannot exceed 500 characters")]
    public required string description_projet { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "url_projet cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxUrlLength, ErrorMessage = "url_projet cannot exceed 150 characters")]
    public required string url_projet { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "status_projet cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxStatusLength, ErrorMessage = "status_projet cannot exceed 50 characters")]
    public required string status_projet { get; init; }

    [Required]
    public DateTime date_debut_projet { get; init; }

    public DateTime? date_fin_projet { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(nom_projet))
        {
            yield return new ValidationResult("nom_projet cannot be null, empty, or whitespace.", new[] { nameof(nom_projet) });
        }
        if (string.IsNullOrWhiteSpace(description_projet))
        {
            yield return new ValidationResult("description_projet cannot be null, empty, or whitespace.", new[] { nameof(description_projet) });
        }
        if (string.IsNullOrWhiteSpace(url_projet))
        {
            yield return new ValidationResult("url_projet cannot be null, empty, or whitespace.", new[] { nameof(url_projet) });
        }
        if (string.IsNullOrWhiteSpace(status_projet))
        {
            yield return new ValidationResult("status_projet cannot be null, empty, or whitespace.", new[] { nameof(status_projet) });
        }
    }
}
public record UpdateProjetDto : IValidatableObject
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "nom_projet cannot exceed 50 characters")]
    public string? nom_projet { get; init; }

    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "description_projet cannot exceed 500 characters")]
    public string? description_projet { get; init; }

    [MaxLength(Constants.MaxUrlLength, ErrorMessage = "url_projet cannot exceed 150 characters")]
    public string? url_projet { get; init; }

    [MaxLength(Constants.MaxStatusLength, ErrorMessage = "status_projet cannot exceed 50 characters")]
    public string? status_projet { get; init; }

    public DateTime? date_debut_projet { get; init; }

    public DateTime? date_fin_projet { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (nom_projet is not null && string.IsNullOrWhiteSpace(nom_projet))
        {
            yield return new ValidationResult("nom_projet cannot be empty or whitespace.", new[] { nameof(nom_projet) });
        }
        if (description_projet is not null && string.IsNullOrWhiteSpace(description_projet))
        {
            yield return new ValidationResult("description_projet cannot be empty or whitespace.", new[] { nameof(description_projet) });
        }
        if (url_projet is not null && string.IsNullOrWhiteSpace(url_projet))
        {
            yield return new ValidationResult("url_projet cannot be empty or whitespace.", new[] { nameof(url_projet) });
        }
        if (status_projet is not null && string.IsNullOrWhiteSpace(status_projet))
        {
            yield return new ValidationResult("status_projet cannot be empty or whitespace.", new[] { nameof(status_projet) });
        }
    }
}