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
    public required List<ReadItemTagDto> Valide { get; init; }
    public required List<ErrorDetail> Error { get; init; }
}
public record CreateItemTagByTagDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_item { get; init; }
}
public record CreateItemTagByItemDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_tag { get; init; }
}
public record CreateItemTagDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_item { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    public required int id_tag { get; init; }
}