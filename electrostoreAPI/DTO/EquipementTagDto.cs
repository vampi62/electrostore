using System.ComponentModel.DataAnnotations;

namespace ElectrostoreAPI.Dto;

public record ReadEquipementTagDto
{
    public int id_equipement { get; init; }
    public int id_tag { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedEquipementTagDto : ReadEquipementTagDto
{
    public ReadEquipementDto? equipement { get; init; }
    public ReadTagDto? tag { get; init; }
}
public record ReadBulkEquipementTagDto
{
    public required List<ReadEquipementTagDto> Valide { get; init; }
    public required List<ErrorDetail> Error { get; init; }
}
public record CreateEquipementTagByTagDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_equipement { get; init; }
}
public record CreateEquipementTagByEquipementDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_tag { get; init; }
}
public record CreateEquipementTagDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_equipement { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    public required int id_tag { get; init; }
}
