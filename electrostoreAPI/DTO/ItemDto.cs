using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadItemDto
{
    public int id_item { get; init; }
    public string nom_item { get; init; }
    public int seuil_min_item { get; init; }
    public string description_item { get; init; }
    public int? id_img { get; init; }
    public ReadItemBoxDto[]? itembox { get; init; }
}
public record CreateItemDto
{
    [Required] public string nom_item { get; init; }
    [Required] public int seuil_min_item { get; init; }
    [Required] public string description_item { get; init; }
    public int? id_img { get; init; }
}
public record UpdateItemDto
{
    public string? nom_item { get; init; }
    public int? seuil_min_item { get; init; }
    public string? description_item { get; init; }
    public int? id_img { get; init; }
}