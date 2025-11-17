using System.ComponentModel.DataAnnotations;
using electrostore.Validators;

namespace electrostore.Dto;

public record ReadItemDto
{
    public int id_item { get; init; }
    public required string reference_name_item { get; init; }
    public required string friendly_name_item { get; init; }
    public int seuil_min_item { get; init; }
    public required string description_item { get; init; }
    public int? id_img { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedItemDto : ReadItemDto
{
    public int item_tags_count { get; init; }
    public int item_boxs_count { get; init; }
    public int command_items_count { get; init; }
    public int projet_items_count { get; init; }
    public int item_documents_count { get; init; }
    public IEnumerable<ReadItemTagDto>? item_tags { get; init; }
    public IEnumerable<ReadItemBoxDto>? item_boxs { get; init; }
    public IEnumerable<ReadCommandItemDto>? command_items { get; init; }
    public IEnumerable<ReadProjetItemDto>? projet_items { get; init; }
    public IEnumerable<ReadItemDocumentDto>? item_documents { get; init; }
}
public record CreateItemDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string reference_name_item { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string friendly_name_item { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int seuil_min_item { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string description_item { get; init; }

    public int? id_img { get; init; }
}
public record UpdateItemDto
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? reference_name_item { get; init; }

    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? friendly_name_item { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? seuil_min_item { get; init; }

    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? description_item { get; init; }

    public int? id_img { get; init; }
}