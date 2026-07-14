using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Validators;
using System.ComponentModel.DataAnnotations;

namespace ElectrostoreAPI.Dto;

public record ReadCommandDto
{
    public int id_command { get; init; }
    public float? prix_command { get; init; }
    public string? url_command { get; init; }
    public CommandStatus status_command { get; init; }
    public DateTime date_command { get; init; }
    public DateTime? date_livraison_command { get; init; }
    public required string tracking_number { get; init; }
    public int id_carrier { get; init; }
    public string? carrier_name { get; init; }
    public bool is_tracking_requested { get; init; }
    public bool is_tracking_validated { get; init; }
    public bool is_active { get; init; }
    public string? shipper_adress { get; init; }
    public string? recipient_adress { get; init; }
    public TrackingStatus? last_status { get; init; }
    public TrackingSubStatus? last_sub_status { get; init; }
    public string? raw_data { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedCommandDto : ReadCommandDto
{
    public int commands_commentaires_count { get; init; }
    public int commands_documents_count { get; init; }
    public int commands_items_count { get; init; }
    public int commands_history_count { get; init; }
    public IEnumerable<ReadCommandCommentaireDto>? commands_commentaires { get; init; }
    public IEnumerable<ReadCommandDocumentDto>? commands_documents { get; init; }
    public IEnumerable<ReadCommandItemDto>? commands_items { get; init; }
    public IEnumerable<ReadCommandHistoryDto>? commands_history { get; init; }
    public ReadCarrierDto? carrier { get; init; }
}
public record CreateCommandDto
{
    [Range(0.0, float.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public float? prix_command { get; init; }

    [MaxLength(Constants.MaxUrlLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalUrl(ErrorMessage = "{0} must be a valid URL.")]
    public string? url_command { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, (int)ProjetStatus.Archived, ErrorMessage = "{0} must be a valid status, between {1} and {2}.")]
    public CommandStatus status_command { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    public required DateTime date_command { get; init; }

    public DateTime? date_livraison_command { get; init; }

    [MaxLength(Constants.MaxTrackingNumberLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? tracking_number { get; init; }

    public int? id_carrier { get; init; }

    public bool? is_tracking_requested { get; init; }
}
public record UpdateCommandDto
{
    [Range(0.0, float.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public float? prix_command { get; init; }

    [MaxLength(Constants.MaxUrlLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalUrl(ErrorMessage = "{0} must be a valid URL.")]
    public string? url_command { get; init; }

    [Range(0, (int)ProjetStatus.Archived, ErrorMessage = "{0} must be a valid status, between {1} and {2}.")]
    public CommandStatus? status_command { get; init; }

    public DateTime? date_command { get; init; }

    public DateTime? date_livraison_command { get; init; }

    [MaxLength(Constants.MaxTrackingNumberLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? tracking_number { get; init; }

    public int? id_carrier { get; init; }

    public bool? is_tracking_requested { get; init; }
}