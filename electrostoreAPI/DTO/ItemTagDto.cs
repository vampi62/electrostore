using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadItemTagDto
{
    public int id_item { get; init; }
    public int id_tag { get; init; }
}
public record CreateItemTagDto
{
    [Required] public int id_item { get; init; }
    [Required] public int id_tag { get; init; }
}