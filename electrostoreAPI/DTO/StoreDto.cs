using System.ComponentModel.DataAnnotations;
using electrostore.Validators;

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
    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters")]
    public required string nom_store { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int xlength_store { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int ylength_store { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string mqtt_name_store { get; init; }
}

public record UpdateStoreDto
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? nom_store { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? xlength_store { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? ylength_store { get; init; }

    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? mqtt_name_store { get; init; }
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