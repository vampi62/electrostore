using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadStoreTagDto
{
    public int id_store { get; init; }
    public int id_tag { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedStoreTagDto : ReadStoreTagDto
{
    public ReadTagDto? tag { get; init; }
    public ReadStoreDto? store { get; init; }
}
public record ReadBulkStoreTagDto
{
    public required List<ReadStoreTagDto> Valide { get; init; }
    public required List<ErrorDetail> Error { get; init; }
}
public record CreateStoreTagByTagDto
{
    [Required]
    public required int id_store { get; init; }
}
public record CreateStoreTagByStoreDto
{
    [Required]
    public required int id_tag { get; init; }
}
public record CreateStoreTagDto
{
    [Required]
    public required int id_store { get; init; }

    [Required]
    public required int id_tag { get; init; }
}