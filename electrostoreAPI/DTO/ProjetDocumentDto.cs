using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadProjetDocumentDto
{
    public int id_projet_document { get; init; }
    public int id_projet { get; init; }
    public required string url_projet_document { get; init; }
    public required string name_projet_document { get; init; }
    public required string type_projet_document { get; init; }
    public decimal size_projet_document { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record CreateProjetDocumentDto : IValidatableObject
{
    [Required]
    public required int id_projet { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "name_projet_document cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "name_projet_document cannot exceed 50 characters")]
    public required string name_projet_document { get; init; }

    [Required]
    public required IFormFile document { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (document.Length == 0)
        {
            yield return new ValidationResult("The file is empty.", new[] { nameof(document) });
        }
        const long maxFileSize = Constants.MaxDocumentSizeMB * 1024 * 1024;
        if (document.Length > maxFileSize)
        {
            yield return new ValidationResult($"The file size cannot exceed {Constants.MaxDocumentSizeMB} MB.", new[] { nameof(document) });
        }
        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".png", ".jpg", ".jpeg", ".gif", ".bmp" };
        var fileExtension = Path.GetExtension(document.FileName).ToLowerInvariant();
        if (!string.IsNullOrEmpty(fileExtension) && !allowedExtensions.Contains(fileExtension))
        {
            yield return new ValidationResult("The file type is not allowed. Allowed types are: .pdf, .doc, .docx, .xls, .xlsx, .ppt, .pptx, .txt, .png, .jpg, .jpeg, .gif, .bmp.", new[] { nameof(document) });
        }
    }
}
public record CreateProjetDocumentByProjetDto : IValidatableObject
{
    [Required]
    [MinLength(1, ErrorMessage = "name_projet_document cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "name_projet_document cannot exceed 50 characters")]
    public required string name_projet_document { get; init; }

    [Required]
    public required IFormFile document { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (document.Length == 0)
        {
            yield return new ValidationResult("The file is empty.", new[] { nameof(document) });
        }
        const long maxFileSize = Constants.MaxDocumentSizeMB * 1024 * 1024;
        if (document.Length > maxFileSize)
        {
            yield return new ValidationResult($"The file size cannot exceed {Constants.MaxDocumentSizeMB} MB.", new[] { nameof(document) });
        }
        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".png", ".jpg", ".jpeg", ".gif", ".bmp" };
        var fileExtension = System.IO.Path.GetExtension(document.FileName).ToLowerInvariant();
        if (!string.IsNullOrEmpty(fileExtension) && !allowedExtensions.Contains(fileExtension))
        {
            yield return new ValidationResult("The file type is not allowed. Allowed types are: .pdf, .doc, .docx, .xls, .xlsx, .ppt, .pptx, .txt, .png, .jpg, .jpeg, .gif, .bmp.", new[] { nameof(document) });
        }
    }
}
public record UpdateProjetDocumentDto : IValidatableObject
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "name_projet_document cannot exceed 50 characters")]
    public string? name_projet_document { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (name_projet_document is not null && string.IsNullOrWhiteSpace(name_projet_document))
        {
            yield return new ValidationResult("name_projet_document cannot be empty or whitespace.", new[] { nameof(name_projet_document) });
        }
    }
}