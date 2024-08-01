using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadStoreTagDto
{
    public int id_store { get; init; }
    public int id_tag { get; init; }
}
public record CreateStoreTagDto
{
    [Required] public int id_store { get; init; }
    [Required] public int id_tag { get; init; }
}