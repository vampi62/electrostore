using System.ComponentModel.DataAnnotations;
using electrostore.Enums;
using electrostore.Validators;

namespace electrostore.Dto;

public record ReadProjetDto
{
    public int id_projet { get; init; }
    public required string nom_projet { get; init; }
    public required string description_projet { get; init; }
    public required string url_projet { get; init; }
    public ProjetStatus status_projet { get; init; }
    public DateTime? date_debut_projet { get; init; }
    public DateTime? date_fin_projet { get; init; }
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
    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? nom_projet { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? description_projet { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxUrlLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? url_projet { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, (int)ProjetStatus.Archived, ErrorMessage = "{0} must be a valid status, between {1} and {2}.")]
    public ProjetStatus status_projet { get; init; }
}
public record UpdateProjetDto
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? nom_projet { get; init; }

    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? description_projet { get; init; }

    [MaxLength(Constants.MaxUrlLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? url_projet { get; init; }

    [Range(0, (int)ProjetStatus.Archived, ErrorMessage = "{0} must be a valid status, between {1} and {2}.")]
    public ProjetStatus? status_projet { get; init; }
}