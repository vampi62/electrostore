using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadBoxTagDto
{
    public int id_tag { get; init; }
    public int id_box { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedBoxTagDto : ReadBoxTagDto
{
    public ReadTagDto? tag { get; init; }
    public ReadBoxDto? box { get; init; }
}
public record ReadBulkBoxTagDto
{
    public required List<ReadBoxTagDto> Valide { get; init; }
    public required List<ErrorDetail> Error { get; init; }
}
public record CreateBoxTagByTagDto
{
    [Required]
    public required int id_box { get; init; }
}
public record CreateBoxTagByBoxDto
{
    [Required]
    public required int id_tag { get; init; }
}
public record CreateBoxTagDto
{
    [Required]
    public required int id_tag { get; init; }

    [Required]
    public required int id_box { get; init; }
}