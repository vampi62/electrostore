using ElectrostoreAPI.Validators;
using System.ComponentModel.DataAnnotations;

namespace ElectrostoreAPI.Dto;

public record ReadZoneDto
{
    public int id_zone { get; init; }
    public required string name_zone { get; init; }
    public string? description_zone { get; init; }
    public int xlength_zone { get; init; }
    public int ylength_zone { get; init; }
    public string? url_picture_zone { get; init; }
    public string? url_thumbnail_zone { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedZoneDto : ReadZoneDto
{
    public int stores_count { get; init; }
    public IEnumerable<ReadStoreDto>? stores { get; init; }
}
public record ReadBulkZoneDto
{
    public required List<ReadZoneDto> Valide { get; init; }
    public required List<ErrorDetail> Error { get; init; }
}
public record CreateZoneDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string name_zone { get; init; }

    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? description_zone { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int xlength_zone { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public required int ylength_zone { get; init; }
}
public record UpdateZoneDto
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? name_zone { get; init; }

    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? description_zone { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? xlength_zone { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "{0} must be greater than or equal to {1}, and less than or equal to {2}.")]
    public int? ylength_zone { get; init; }
}
public record CreateZonePictureDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [FileSize(nameof(Constants.MaxImageSizeMB), ErrorMessage = "{0} cannot exceed {1} MB in size.")]
    [FileType(nameof(Constants.AllowedImageMimeTypes),
        ErrorMessage = "{0} has an invalid file type, allowed types are: [{1}], and extensions are: [{2}].")]
    public required IFormFile img_file { get; init; }
}
