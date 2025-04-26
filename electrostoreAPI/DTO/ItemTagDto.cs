using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadItemTagDto
{
    public int id_item { get; init; }
    public int id_tag { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedItemTagDto : ReadItemTagDto
{
    public ReadItemDto? item { get; init; }
    public ReadTagDto? tag { get; init; }
}
public record ReadBulkItemTagDto
{
    public List<ReadItemTagDto> Valide { get; init; }
    public List<ErrorDetail> Error { get; init; }
}
public record CreateItemTagByTagDto
{
    [Required]
    public int id_item { get; init; }
}
public record CreateItemTagByItemDto
{
    [Required]
    public int id_tag { get; init; }
}
public record CreateItemTagDto
{
    [Required]
    public int id_item { get; init; }

    [Required]
    public int id_tag { get; init; }
}