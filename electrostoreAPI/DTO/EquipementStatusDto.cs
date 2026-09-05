using ElectrostoreAPI.Enums;
using System.ComponentModel.DataAnnotations;

namespace ElectrostoreAPI.Dto;

public record ReadEquipementStatusDto
{
    public int id_equipement_status { get; init; }
    public int id_equipement { get; init; }
    public EquipementStatus status_equipement { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedEquipementStatusDto : ReadEquipementStatusDto
{
    public ReadEquipementDto? equipement { get; init; }
}
public record CreateEquipementStatusDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int id_equipement { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, (int)EquipementStatus.Retired, ErrorMessage = "{0} must be a valid EquipementStatus value, between {1} and {2}.")]
    public required EquipementStatus status_equipement { get; init; }
}
