using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadItemBoxDto
{
    public int id_box { get; init; }
    public int id_item { get; init; }
    public int qte_itembox { get; init; }
    public int seuil_max_itemitembox { get; init; }
}
public record CreateItemBoxByBoxDto
{
    [Required] public int id_item { get; init; }
    [Required] public int qte_itembox { get; init; }
    [Required] public int seuil_max_itemitembox { get; init; }
}
public record CreateItemBoxByItemDto
{
    [Required] public int id_box { get; init; }
    [Required] public int qte_itembox { get; init; }
    [Required] public int seuil_max_itemitembox { get; init; }
}
public record CreateItemBoxDto
{
    [Required] public int id_box { get; init; }
    [Required] public int id_item { get; init; }
    [Required] public int qte_itembox { get; init; }
    [Required] public int seuil_max_itemitembox { get; init; }
}
public record UpdateItemBoxDto
{
    public int? new_id_box { get; init; }
    public int? qte_itembox { get; init; }
    public int? seuil_max_itemitembox { get; init; }
}