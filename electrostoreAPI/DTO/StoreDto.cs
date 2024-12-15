using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadStoreDto
{
    public int id_store { get; init; }
    public string nom_store { get; init; }
    public int xlength_store { get; init; }
    public int ylength_store { get; init; }
    public string mqtt_name_store { get; init; }
}
public record ReadExtendedStoreDto
{
    public int id_store { get; init; }
    public string nom_store { get; init; }
    public int xlength_store { get; init; }
    public int ylength_store { get; init; }
    public string mqtt_name_store { get; init; }
    public int boxs_count { get; init; }
    public int leds_count { get; init; }
    public int stores_tags_count { get; init; }
    public IEnumerable<ReadBoxDto>? boxs { get; init; }
    public IEnumerable<ReadLedDto>? leds { get; init; }
    public IEnumerable<ReadStoreTagDto>? stores_tags { get; init; }
}

public record CreateStoreDto : IValidatableObject
{
    [Required]
    [MinLength(1, ErrorMessage = "nom_store cannot be empty or whitespace.")]
    [MaxLength(50, ErrorMessage = "nom_store cannot exceed 50 characters")]
    public string nom_store { get; init; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "xlength_store must be greater than 0.")]
    public int xlength_store { get; init; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "ylength_store must be greater than 0.")]
    public int ylength_store { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "mqtt_name_store cannot be empty or whitespace.")]
    [MaxLength(50, ErrorMessage = "mqtt_name_store cannot exceed 50 characters")]
    public string mqtt_name_store { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(nom_store))
        {
            yield return new ValidationResult("nom_store cannot be null, empty, or whitespace.", new[] { nameof(nom_store) });
        }

        if (string.IsNullOrWhiteSpace(mqtt_name_store))
        {
            yield return new ValidationResult("mqtt_name_store cannot be null, empty, or whitespace.", new[] { nameof(mqtt_name_store) });
        }
    }
}

public record UpdateStoreDto : IValidatableObject
{
    [MaxLength(50, ErrorMessage = "nom_store cannot exceed 50 characters.")]
    public string? nom_store { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "xlength_store must be greater than 0.")]
    public int? xlength_store { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "ylength_store must be greater than 0.")]
    public int? ylength_store { get; init; }

    [MaxLength(50, ErrorMessage = "mqtt_name_store cannot exceed 50 characters.")]
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