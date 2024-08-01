using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadIAImgDto
{
    public int id_ia { get; init; }
    public int id_img { get; init; }
}
public record CreateIAImgDto
{
    [Required] public int id_ia { get; init; }
    [Required] public int id_img { get; init; }
}