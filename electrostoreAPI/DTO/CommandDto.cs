using System.ComponentModel.DataAnnotations;
using electrostore.Validators;

namespace electrostore.Dto;

public record ReadCommandDto
{
    public int id_command { get; init; }
    public float prix_command { get; init; }
    public required string url_command { get; init; }
    public required string status_command { get; init; }
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
public record CreateCommandDto
{
    [Required]
    [Range(0.0, float.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public float prix_command { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxUrlLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [Url(ErrorMessage = "{0} must be a valid URL.")]
    public string? url_command { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxStatusLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? status_command { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    public DateTime date_command { get; init; }

    public DateTime? date_livraison_command { get; init; }
}
public record UpdateCommandDto
{
    [Range(0.0, float.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public float? prix_command { get; init; }

    [MaxLength(Constants.MaxUrlLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    [Url(ErrorMessage = "{0} must be a valid URL.")]
    public string? url_command { get; init; }

    [MaxLength(Constants.MaxStatusLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? status_command { get; init; }

    public DateTime? date_command { get; init; }

    public DateTime? date_livraison_command { get; init; }
}