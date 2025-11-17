using System.ComponentModel.DataAnnotations;
using electrostore.Validators;
using System.Text.Json.Serialization;

namespace electrostore.Dto;

public record ReadTagDto
{
    public int id_tag { get; init; }
    public required string nom_tag { get; init; }
    public int poids_tag { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedTagDto : ReadTagDto
{
    public int stores_tags_count { get; init; }
    public int items_tags_count { get; init; }
    public int boxs_tags_count { get; init; }
    public IEnumerable<ReadStoreTagDto>? stores_tags { get; init; }
    public IEnumerable<ReadItemTagDto>? items_tags { get; init; }
    public IEnumerable<ReadBoxTagDto>? boxs_tags { get; init; }
}

public record ReadBulkTagDto
{
    public required List<ReadTagDto> Valide { get; init; }
    public required List<ErrorDetail> Error { get; init; }
}

public record CreateTagDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string nom_tag { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int poids_tag { get; init; }
}
public record UpdateTagDto
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? nom_tag { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? poids_tag { get; init; }
}