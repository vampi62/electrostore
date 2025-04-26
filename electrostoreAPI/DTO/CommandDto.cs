using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadCommandDto
{
    public int id_command { get; init; }
    public float prix_command { get; init; }
    public string url_command { get; init; }
    public string status_command { get; init; }
    public DateTime date_command { get; init; }
    public DateTime? date_livraison_command { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedCommandDto : ReadCommandDto
{
    public int commands_commentaires_count { get; init; }
    public int commands_documents_count { get; init; }
    public int commands_items_count { get; init; }
    public IEnumerable<ReadCommandCommentaireDto>? commands_commentaires { get; init; }
    public IEnumerable<ReadCommandDocumentDto>? commands_documents { get; init; }
    public IEnumerable<ReadCommandItemDto>? commands_items { get; init; }
}
public record CreateCommandDto : IValidatableObject
{
    [Required]
    [Range(0.0, float.MaxValue, ErrorMessage = "prix_command must be greater than 0.")]
    public float prix_command { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "url_command cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxUrlLength, ErrorMessage = "url_command cannot exceed 150 characters")]
    public string url_command { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "status_command cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxStatusLength, ErrorMessage = "status_command cannot exceed 50 characters")]
    public string status_command { get; init; }

    [Required]
    public DateTime date_command { get; init; }

    public DateTime? date_livraison_command { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(url_command))
        {
            yield return new ValidationResult("url_command cannot be null, empty, or whitespace.", new[] { nameof(url_command) });
        }
        if (string.IsNullOrWhiteSpace(status_command))
        {
            yield return new ValidationResult("status_command cannot be null, empty, or whitespace.", new[] { nameof(status_command) });
        }
    }
}
public record UpdateCommandDto : IValidatableObject
{
    [Range(0.0, float.MaxValue, ErrorMessage = "prix_command must be greater than 0.")]
    public float? prix_command { get; init; }

    [MaxLength(Constants.MaxUrlLength, ErrorMessage = "url_command cannot exceed 150 characters")]
    public string? url_command { get; init; }

    [MaxLength(Constants.MaxStatusLength, ErrorMessage = "status_command cannot exceed 50 characters")]
    public string? status_command { get; init; }

    public DateTime? date_command { get; init; }

    public DateTime? date_livraison_command { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (url_command is not null && string.IsNullOrWhiteSpace(url_command))
        {
            yield return new ValidationResult("url_command cannot be empty or whitespace.", new[] { nameof(url_command) });
        }
        if (status_command is not null && string.IsNullOrWhiteSpace(status_command))
        {
            yield return new ValidationResult("status_command cannot be empty or whitespace.", new[] { nameof(status_command) });
        }
    }
}