using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadItemDto
{
    public int id_item { get; init; }
    public string nom_item { get; init; }
    public int seuil_min_item { get; init; }
    public string description_item { get; init; }
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
public record CreateItemDto : IValidatableObject
{
    [Required]
    [MinLength(1, ErrorMessage = "nom_item cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "nom_item cannot exceed 50 characters")]
    public string nom_item { get; init; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "seuil_min_item must be greater than or equal to 0.")]
    public int seuil_min_item { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "description_item cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "description_item cannot exceed 500 characters")]
    public string description_item { get; init; }

    public int? id_img { get; init; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(nom_item))
        {
            yield return new ValidationResult("nom_item cannot be null, empty, or whitespace.", new[] { nameof(nom_item) });
        }
        if (string.IsNullOrWhiteSpace(description_item))
        {
            yield return new ValidationResult("description_item cannot be null, empty, or whitespace.", new[] { nameof(description_item) });
        }
    }
}
public record UpdateItemDto : IValidatableObject
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "nom_item cannot exceed 50 characters")]
    public string? nom_item { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "seuil_min_item must be greater than or equal to 0.")]
    public int? seuil_min_item { get; init; }

    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "description_item cannot exceed 500 characters")]
    public string? description_item { get; init; }

    public int? id_img { get; init; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (nom_item is not null && string.IsNullOrWhiteSpace(nom_item))
        {
            yield return new ValidationResult("nom_item cannot be null, empty, or whitespace.", new[] { nameof(nom_item) });
        }
        if (description_item is not null && string.IsNullOrWhiteSpace(description_item))
        {
            yield return new ValidationResult("description_item cannot be null, empty, or whitespace.", new[] { nameof(description_item) });
        }
    }
}