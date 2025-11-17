using System.ComponentModel.DataAnnotations;
using electrostore.Validators;

namespace electrostore.Dto;

public record ReadImgDto
{
    public int id_img { get; init; }
    public required string nom_img { get; init; }
    public required string url_picture_img { get; init; }
    public required string url_thumbnail_img { get; init; }
    public required string description_img { get; init; }
    public int id_item { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record CreateImgByItemDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string nom_img { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string description_img { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [FileSize(Constants.MaxDocumentSizeMB, ErrorMessage = "{0} cannot exceed {1} MB in size.")]
    [FileExtension([".png", ".webp", ".jpg", ".jpeg", ".gif", ".bmp"],
        ErrorMessage = "{0} has an invalid file type, allowed types are: [{1}].")]
    public required IFormFile img_file { get; init; }
}
public record CreateImgDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string nom_img { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string description_img { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    public int id_item { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [FileSize(Constants.MaxDocumentSizeMB, ErrorMessage = "{0} cannot exceed {1} MB in size.")]
    [FileExtension([".png", ".webp", ".jpg", ".jpeg", ".gif", ".bmp"],
        ErrorMessage = "{0} has an invalid file type, allowed types are: [{1}].")]
    public required IFormFile img_file { get; init; }
}
public record UpdateImgDto
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? nom_img { get; init; }

    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? description_img { get; init; }
}