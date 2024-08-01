using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadBoxTagDto
{
    public int id_tag { get; init; }
    public int id_box { get; init; }
}
public record CreateBoxTagDto
{
    [Required] public int id_tag { get; init; }
    [Required] public int id_box { get; init; }
}