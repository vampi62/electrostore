using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Validators;
using System.ComponentModel.DataAnnotations;

namespace ElectrostoreAPI.Dto;

public record ReadProjetDto
{
    public int id_project { get; init; }
    public required string name_project { get; init; }
    public string? description_project { get; init; }
    public string? url_project { get; init; }
    public ProjetStatus status_project { get; init; }
    public DateTime? date_start_project { get; init; }
    public DateTime? date_end_project { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedProjetDto : ReadProjetDto
{
    public int project_comments_count { get; init; }
    public int project_documents_count { get; init; }
    public int project_items_count { get; init; }
    public int project_tags_count { get; init; }
    public int project_status_history_count { get; init; }
    public IEnumerable<ReadProjetCommentaireDto>? project_comments { get; init; }
    public IEnumerable<ReadProjetDocumentDto>? project_documents { get; init; }
    public IEnumerable<ReadProjetItemDto>? project_items { get; init; }
    public IEnumerable<ReadProjetProjetTagDto>? project_tags { get; init; }
    public IEnumerable<ReadProjetStatusDto>? project_status_history { get; init; }
    
}
public record CreateProjetDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string name_project { get; init; }

    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? description_project { get; init; }

    [MaxLength(Constants.MaxUrlLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalUrl(ErrorMessage = "{0} must be a valid URL.")]
    public string? url_project { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, (int)ProjetStatus.Archived, ErrorMessage = "{0} must be a valid status, between {1} and {2}.")]
    public required ProjetStatus status_project { get; init; }
}
public record UpdateProjetDto
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? name_project { get; init; }

    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? description_project { get; init; }

    [MaxLength(Constants.MaxUrlLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalUrl(ErrorMessage = "{0} must be a valid URL.")]
    public string? url_project { get; init; }

    [Range(0, (int)ProjetStatus.Archived, ErrorMessage = "{0} must be a valid status, between {1} and {2}.")]
    public ProjetStatus? status_project { get; init; }
}