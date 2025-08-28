using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadCommandItemDto
{
    public int id_item { get; init; }
    public int id_command { get; init; }
    public int qte_command_item { get; init; }
    public float prix_command_item { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedCommandItemDto : ReadCommandItemDto
{
    public ReadItemDto? item { get; init; }
    public ReadCommandDto? command { get; init; }
}
public record ReadBulkCommandItemDto
{
    public required List<ReadCommandItemDto> Valide { get; init; }
    public required List<ErrorDetail> Error { get; init; }
}
public record CreateCommandItemByCommandDto
{
    [Required]
    public required int id_item { get; init; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "qte_projet_item must be greater than 0.")]
    public required int qte_command_item { get; init; }

    [Required]
    [Range(0.0, float.MaxValue, ErrorMessage = "prix_command_item must be greater than 0.")]
    public required float prix_command_item { get; init; }
}
public record CreateCommandItemByItemDto
{
    [Required]
    public required int id_command { get; init; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "qte_projet_item must be greater than 0.")]
    public required int qte_command_item { get; init; }

    [Required]
    [Range(0.0, float.MaxValue, ErrorMessage = "prix_command_item must be greater than 0.")]
    public required float prix_command_item { get; init; }
}
public record CreateCommandItemDto
{
    [Required]
    public required int id_item { get; init; }

    [Required]
    public required int id_command { get; init; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "qte_projet_item must be greater than 0.")]
    public required int qte_command_item { get; init; }

    [Required]
    [Range(0.0, float.MaxValue, ErrorMessage = "prix_command_item must be greater than or equal to 0.")]
    public required float prix_command_item { get; init; }
}
public record UpdateCommandItemDto
{
    [Range(1, int.MaxValue, ErrorMessage = "qte_projet_item must be greater than 0.")]
    public int? qte_command_item { get; init; }

    [Range(0.0, float.MaxValue, ErrorMessage = "prix_command_item must be greater than or equal to 0.")]
    public float? prix_command_item { get; init; }
}