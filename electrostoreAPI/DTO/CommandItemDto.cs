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
    [Required(ErrorMessage = "{0} is required.")]
    public int id_item { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int qte_command_item { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0.0, float.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public float prix_command_item { get; init; }
}
public record CreateCommandItemByItemDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public int id_command { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int qte_command_item { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0.0, float.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public float prix_command_item { get; init; }
}
public record CreateCommandItemDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public int id_item { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    public int id_command { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int qte_command_item { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0.0, float.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public float prix_command_item { get; init; }
}
public record UpdateCommandItemDto
{
    [Range(1, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? qte_command_item { get; init; }

    [Range(0.0, float.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public float? prix_command_item { get; init; }
}