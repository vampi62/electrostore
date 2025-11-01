using System.ComponentModel.DataAnnotations;
using electrostore.Enums;

namespace electrostore.Dto;

public record ReadProjetDto
{
    public int id_projet { get; init; }
    public required string nom_projet { get; init; }
    public required string description_projet { get; init; }
    public required string url_projet { get; init; }
    public ProjetStatus status_projet { get; init; }
    public DateTime date_debut_projet { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedProjetDto : ReadProjetDto
{
    public int projets_commentaires_count { get; init; }
    public int projets_documents_count { get; init; }
    public int projets_items_count { get; init; }
    public int projets_tags_count { get; init; }
    public int projets_status_history_count { get; init; }
    public IEnumerable<ReadProjetCommentaireDto>? projets_commentaires { get; init; }
    public IEnumerable<ReadProjetDocumentDto>? projets_documents { get; init; }
    public IEnumerable<ReadProjetItemDto>? projets_items { get; init; }
    public IEnumerable<ReadProjetProjetTagDto>? projets_projet_tags { get; init; }
    public IEnumerable<ReadProjetStatusDto>? projets_status_history { get; init; }
    
}
public record CreateProjetDto
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
    public required ProjetStatus status_projet { get; init; }

    [Required]
    public required DateTime date_debut_projet { get; init; }
}
public record UpdateProjetDto : IValidatableObject
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "nom_projet cannot exceed 50 characters")]
    public string? nom_projet { get; init; }

    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "description_projet cannot exceed 500 characters")]
    public string? description_projet { get; init; }

    [MaxLength(Constants.MaxUrlLength, ErrorMessage = "url_projet cannot exceed 150 characters")]
    public string? url_projet { get; init; }

    public ProjetStatus? status_projet { get; init; }

    public DateTime? date_debut_projet { get; init; }

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
    }
}