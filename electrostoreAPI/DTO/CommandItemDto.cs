using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadCommandItemDto
{
    public int id_item { get; init; }
    public int id_command { get; init; }
    public int qte_commanditem { get; init; }
    public float prix_commanditem { get; init; }
    public ReadItemDto item { get; init; }
}
public record CreateCommandItemByCommandDto
{
    [Required] public int qte_commanditem { get; init; }
    [Required] public float prix_commanditem { get; init; }
}
public record CreateCommandItemByItemDto
{
    [Required] public int qte_commanditem { get; init; }
    [Required] public float prix_commanditem { get; init; }
}
public record CreateCommandItemDto
{
    [Required] public int id_item { get; init; }
    [Required] public int id_command { get; init; }
    [Required] public int qte_commanditem { get; init; }
    [Required] public float prix_commanditem { get; init; }
}
public record UpdateCommandItemDto
{
    public int? qte_commanditem { get; init; }
    public float? prix_commanditem { get; init; }
}