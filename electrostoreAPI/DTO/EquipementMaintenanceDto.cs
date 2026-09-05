using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Validators;
using System.ComponentModel.DataAnnotations;

namespace ElectrostoreAPI.Dto;

public record ReadEquipementMaintenanceDto
{
    public int id_equipement_maintenance { get; init; }
    public int id_equipement { get; init; }
    public int? id_user { get; init; }
    public EquipementMaintenanceType type_equipement_maintenance { get; init; }
    public DateTime date_planned_equipement_maintenance { get; init; }
    public DateTime? date_done_equipement_maintenance { get; init; }
    public string? description_equipement_maintenance { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedEquipementMaintenanceDto : ReadEquipementMaintenanceDto
{
    public ReadEquipementDto? equipement { get; init; }
    public ReadUserDto? user { get; init; }
}
public record CreateEquipementMaintenanceByEquipementDto
{
    public int? id_user { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, (int)EquipementMaintenanceType.Inspection, ErrorMessage = "{0} must be a valid EquipementMaintenanceType value, between {1} and {2}.")]
    public required EquipementMaintenanceType type_equipement_maintenance { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    public required DateTime date_planned_equipement_maintenance { get; init; }

    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? description_equipement_maintenance { get; init; }
}
public record CreateEquipementMaintenanceDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_equipement { get; init; }

    public int? id_user { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, (int)EquipementMaintenanceType.Inspection, ErrorMessage = "{0} must be a valid EquipementMaintenanceType value, between {1} and {2}.")]
    public required EquipementMaintenanceType type_equipement_maintenance { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    public required DateTime date_planned_equipement_maintenance { get; init; }

    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? description_equipement_maintenance { get; init; }
}
public record UpdateEquipementMaintenanceDto
{
    public int? id_user { get; init; }

    [Range(0, (int)EquipementMaintenanceType.Inspection, ErrorMessage = "{0} must be a valid EquipementMaintenanceType value, between {1} and {2}.")]
    public EquipementMaintenanceType? type_equipement_maintenance { get; init; }

    public DateTime? date_planned_equipement_maintenance { get; init; }

    public DateTime? date_done_equipement_maintenance { get; init; }

    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? description_equipement_maintenance { get; init; }
}
