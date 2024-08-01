using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadBoxDto
{
    public int id_box { get; init; }
    public int xstart_box { get; init; }
    public int ystart_box { get; init; }
    public int xend_box { get; init; }
    public int yend_box { get; init; }
    public int id_store { get; init; }
}
public record CreateBoxByStoreDto
{
    [Required] public int xstart_box { get; init; }
    [Required] public int ystart_box { get; init; }
    [Required] public int xend_box { get; init; }
    [Required] public int yend_box { get; init; }
}
public record CreateBoxDto
{
    [Required] public int xstart_box { get; init; }
    [Required] public int ystart_box { get; init; }
    [Required] public int xend_box { get; init; }
    [Required] public int yend_box { get; init; }
    [Required] public int id_store { get; init; }
}
public record UpdateBoxDto
{
    public int? xstart_box { get; init; }
    public int? ystart_box { get; init; }
    public int? xend_box { get; init; }
    public int? yend_box { get; init; }
    public int? new_id_store { get; init; }
}