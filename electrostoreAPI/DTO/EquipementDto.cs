using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Validators;
using System.ComponentModel.DataAnnotations;

namespace ElectrostoreAPI.Dto;

public record ReadEquipementDto
{
    public int id_equipement { get; init; }
    public required string reference_name_equipement { get; init; }
    public required string friendly_name_equipement { get; init; }
    public string? description_equipement { get; init; }
    public EquipementStatus status_equipement { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedEquipementDto : ReadEquipementDto
{
    public int equipement_tags_count { get; init; }
    public int equipement_boxs_count { get; init; }
    public int equipement_documents_count { get; init; }
    public int equipement_maintenances_count { get; init; }
    public int equipement_status_history_count { get; init; }
    public int equipement_comments_count { get; init; }
    public IEnumerable<ReadEquipementTagDto>? equipement_tags { get; init; }
    public IEnumerable<ReadEquipementBoxDto>? equipement_boxs { get; init; }
    public IEnumerable<ReadEquipementDocumentDto>? equipement_documents { get; init; }
    public IEnumerable<ReadEquipementMaintenanceDto>? equipement_maintenances { get; init; }
    public IEnumerable<ReadEquipementStatusDto>? equipement_status_history { get; init; }
    public IEnumerable<ReadEquipementCommentDto>? equipement_comments { get; init; }
}
public record CreateEquipementDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string reference_name_equipement { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string friendly_name_equipement { get; init; }

    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? description_equipement { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, (int)EquipementStatus.Retired, ErrorMessage = "{0} must be a valid EquipementStatus value, between {1} and {2}.")]
    public required EquipementStatus status_equipement { get; init; }
}
public record UpdateEquipementDto
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? reference_name_equipement { get; init; }

    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? friendly_name_equipement { get; init; }

    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? description_equipement { get; init; }

    [Range(0, (int)EquipementStatus.Retired, ErrorMessage = "{0} must be a valid EquipementStatus value, between {1} and {2}.")]
    public EquipementStatus? status_equipement { get; init; }
}
