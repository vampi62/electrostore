using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadStoreDto
{
    public int id_store { get; init; }
    public required string nom_store { get; init; }
    public int xlength_store { get; init; }
    public int ylength_store { get; init; }
    public required string mqtt_name_store { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedStoreDto : ReadStoreDto
{
    public int boxs_count { get; init; }
    public int leds_count { get; init; }
    public int stores_tags_count { get; init; }
    public IEnumerable<ReadBoxDto>? boxs { get; init; }
    public IEnumerable<ReadLedDto>? leds { get; init; }
    public IEnumerable<ReadStoreTagDto>? stores_tags { get; init; }
}

public record CreateStoreDto
{
    [Required]
    [MinLength(1, ErrorMessage = "nom_store cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "nom_store cannot exceed 50 characters")]
    public required string nom_store { get; init; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "xlength_store must be greater than 0.")]
    public required int xlength_store { get; init; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "ylength_store must be greater than 0.")]
    public required int ylength_store { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "mqtt_name_store cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "mqtt_name_store cannot exceed 50 characters")]
    public required string mqtt_name_store { get; init; }
}

public record UpdateStoreDto : IValidatableObject
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "nom_store cannot exceed 50 characters.")]
    public string? nom_store { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "xlength_store must be greater than 0.")]
    public int? xlength_store { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "ylength_store must be greater than 0.")]
    public int? ylength_store { get; init; }

    [MaxLength(Constants.MaxNameLength, ErrorMessage = "mqtt_name_store cannot exceed 50 characters.")]
    public string? mqtt_name_store { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (nom_store is not null && string.IsNullOrWhiteSpace(nom_store))
        {
            yield return new ValidationResult("nom_store cannot be empty or whitespace.", new[] { nameof(nom_store) });
        }

        if (mqtt_name_store is not null && string.IsNullOrWhiteSpace(mqtt_name_store))
        {
            yield return new ValidationResult("mqtt_name_store cannot be empty or whitespace.", new[] { nameof(mqtt_name_store) });
        }
    }
}

public record ReadStoreCompleteDto
{
    public required ReadStoreDto store { get; init; }
    public ReadBulkLedDto? leds { get; init; }
    public ReadBulkBoxDto? boxs { get; init; }
}
public record CreateStoreCompleteDto
{
    public required CreateStoreDto store { get; init; }
    public IEnumerable<CreateBoxByStoreDto>? boxs { get; init; }
    public IEnumerable<CreateLedByStoreDto>? leds { get; init; }
}

public record UpdateStoreCompleteDto
{
    public required UpdateStoreDto store { get; init; }
    public IEnumerable<UpdateBulkBoxByStoreDto>? boxs { get; init; }
    public IEnumerable<UpdateBulkLedByStoreDto>? leds { get; init; }
}