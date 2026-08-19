using System.ComponentModel.DataAnnotations;

namespace ElectrostoreAPI.Dto;

public record ReadEquipementBoxDto
{
    public int id_box { get; init; }
    public int id_equipement { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedEquipementBoxDto : ReadEquipementBoxDto
{
    public ReadEquipementDto? equipement { get; init; }
    public ReadBoxDto? box { get; init; }
}
public record CreateEquipementBoxByBoxDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_equipement { get; init; }
}
public record CreateEquipementBoxByEquipementDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_box { get; init; }
}
public record CreateEquipementBoxDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_box { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    public required int id_equipement { get; init; }
}
