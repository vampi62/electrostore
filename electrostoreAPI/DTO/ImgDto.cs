using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadImgDto
{
    public int id_img { get; init; }
    public string nom_img { get; init; }
    public string url_picture_img { get; init; }
    public string url_thumbnail_img { get; init; }
    public string description_img { get; init; }
    public int id_item { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record CreateImgByItemDto : IValidatableObject
{
    [Required]
    [MinLength(1, ErrorMessage = "nom_img cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "nom_img cannot exceed 50 characters")]
    public string nom_img { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "description_img cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "description_img cannot exceed 500 characters")]
    public string description_img { get; init; }

    [Required]
    public IFormFile img_file { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(nom_img))
        {
            yield return new ValidationResult("nom_img cannot be null, empty, or whitespace.", new[] { nameof(nom_img) });
        }
        if (string.IsNullOrWhiteSpace(description_img))
        {
            yield return new ValidationResult("description_img cannot be null, empty, or whitespace.", new[] { nameof(description_img) });
        }
        if (img_file is not null)
        {
            const long maxFileSize = Constants.MaxDocumentSizeMB * 1024 * 1024;
            if (img_file.Length > maxFileSize)
            {
                yield return new ValidationResult($"The file size cannot exceed {Constants.MaxDocumentSizeMB} MB.", new[] { nameof(img_file) });
            }
            var allowedExtensions = new[] { ".tiff", ".jfif", ".bmp", ".pjp", ".apng", ".jpeg", ".png", ".webp", ".svgz", ".jpg", ".heic", ".gif", ".svg", ".heif", ".ico", ".xbm", ".dib", ".tif", ".pjpeg", ".avif" };
            var fileExtension = System.IO.Path.GetExtension(img_file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
            {
                yield return new ValidationResult("The file type is not allowed. Allowed types are: " + string.Join(", ", allowedExtensions) + ".", new[] { nameof(img_file) });
            }
        }
    }
}
public record CreateImgDto : IValidatableObject
{
    [Required]
    [MinLength(1, ErrorMessage = "nom_img cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "nom_img cannot exceed 50 characters")]
    public string nom_img { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "description_img cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "description_img cannot exceed 500 characters")]
    public string description_img { get; init; }

    [Required]
    public int id_item { get; init; }

    [Required]
    public IFormFile img_file { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(nom_img))
        {
            yield return new ValidationResult("nom_img cannot be null, empty, or whitespace.", new[] { nameof(nom_img) });
        }
        if (string.IsNullOrWhiteSpace(description_img))
        {
            yield return new ValidationResult("description_img cannot be null, empty, or whitespace.", new[] { nameof(description_img) });
        }
        if (img_file is not null)
        {
            const long maxFileSize = Constants.MaxDocumentSizeMB * 1024 * 1024;
            if (img_file.Length > maxFileSize)
            {
                yield return new ValidationResult($"The file size cannot exceed {Constants.MaxDocumentSizeMB} MB.", new[] { nameof(img_file) });
            }
            var allowedExtensions = new[] { ".tiff", ".jfif", ".bmp", ".pjp", ".apng", ".jpeg", ".png", ".webp", ".svgz", ".jpg", ".heic", ".gif", ".svg", ".heif", ".ico", ".xbm", ".dib", ".tif", ".pjpeg", ".avif" };
            var fileExtension = System.IO.Path.GetExtension(img_file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
            {
                yield return new ValidationResult("The file type is not allowed. Allowed types are: " + string.Join(", ", allowedExtensions) + ".", new[] { nameof(img_file) });
            }
        }
    }
}
public record UpdateImgDto : IValidatableObject
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "nom_img cannot exceed 50 characters")]
    public string? nom_img { get; init; }

    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "description_img cannot exceed 500 characters")]
    public string? description_img { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (nom_img is not null && string.IsNullOrWhiteSpace(nom_img))
        {
            yield return new ValidationResult("nom_img cannot be null, empty, or whitespace.", new[] { nameof(nom_img) });
        }
        if (description_img is not null && string.IsNullOrWhiteSpace(description_img))
        {
            yield return new ValidationResult("description_img cannot be null, empty, or whitespace.", new[] { nameof(description_img) });
        }
    }
}