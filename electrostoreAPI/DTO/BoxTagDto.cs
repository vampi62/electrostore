using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadBoxTagDto
{
    public int id_tag { get; init; }
    public int id_box { get; init; }
}
public record ReadExtendedBoxTagDto
{
    public int id_tag { get; init; }
    public int id_box { get; init; }
    public ReadTagDto? tag { get; init; }
    public ReadBoxDto? box { get; init; }
}
public record ReadBulkBoxTagDto
{
    public List<ReadBoxTagDto> Valide { get; init; }
    public List<ErrorDetail> Error { get; init; }
}
public record CreateBoxTagByTagDto
{
    [Required]
    public int id_box { get; init; }
}
public record CreateBoxTagByBoxDto
{
    [Required]
    public int id_tag { get; init; }
}
public record CreateBoxTagDto
{
    [Required]
    public int id_tag { get; init; }

    [Required]
    public int id_box { get; init; }
}