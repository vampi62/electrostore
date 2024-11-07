using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadProjetItemDto
{
    public int id_projet { get; init; }
    public int id_item { get; init; }
    public int qte_projet_item { get; init; }
    public ReadItemDto item { get; init; }
}
public record CreateProjetItemByProjetDto
{
    [Required] public int qte_projet_item { get; init; }
}
public record CreateProjetItemByItemDto
{
    [Required] public int qte_projet_item { get; init; }
}
public record CreateProjetItemDto
{
    [Required] public int id_projet { get; init; }
    [Required] public int id_item { get; init; }
    [Required] public int qte_projet_item { get; init; }
}
public record UpdateProjetItemDto
{
    public int? qte_projet_item { get; init; }
}