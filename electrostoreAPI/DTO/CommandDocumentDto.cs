using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadCommandDocumentDto
{
    public int id_command_document { get; init; }
    public int id_command { get; init; }
    public string url_command_document { get; init; }
    public string name_command_document { get; init; }
    public string type_command_document { get; init; }
    public decimal size_command_document { get; init; }
    public DateTime date_command_document { get; init; }
}
public record CreateCommandDocumentDto : IValidatableObject
{
    [Required]
    public int id_command { get; init; }
    
    [Required]
    [MinLength(1, ErrorMessage = "name_command_document cannot be empty or whitespace.")]
    [MaxLength(50, ErrorMessage = "name_command_document cannot exceed 50 characters")]
    public string name_command_document { get; init; }
    
    [Required]
    public IFormFile document { get; init; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(name_command_document))
        {
            yield return new ValidationResult("name_command_document cannot be null, empty, or whitespace.", new[] { nameof(name_command_document) });
        }
        if (document is not null)
        {
            if (document.Length == 0)
            {
                yield return new ValidationResult("The file is empty.", new[] { nameof(document) });
            }
            const long maxFileSize = 5 * 1024 * 1024; // 5 MB
            if (document.Length > maxFileSize)
            {
                yield return new ValidationResult($"The file size cannot exceed {maxFileSize / (1024 * 1024)} MB.", new[] { nameof(document) });
            }
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".png", ".jpg", ".jpeg", ".gif", ".bmp" };
            var fileExtension = System.IO.Path.GetExtension(document.FileName).ToLowerInvariant();
            if (!string.IsNullOrEmpty(fileExtension) && !allowedExtensions.Contains(fileExtension))
            {
                yield return new ValidationResult("The file type is not allowed. Allowed types are: .pdf, .doc, .docx, .xls, .xlsx, .ppt, .pptx, .txt, .png, .jpg, .jpeg, .gif, .bmp.", new[] { nameof(document) });
            }
        }
    }
}
public record CreateCommandDocumentByCommandDto : IValidatableObject
{
    [Required]
    [MinLength(1, ErrorMessage = "name_command_document cannot be empty or whitespace.")]
    [MaxLength(50, ErrorMessage = "name_command_document cannot exceed 50 characters")]
    public string name_command_document { get; init; }
    
    [Required]
    public IFormFile document { get; init; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(name_command_document))
        {
            yield return new ValidationResult("name_command_document cannot be null, empty, or whitespace.", new[] { nameof(name_command_document) });
        }
        if (document is not null)
        {
            if (document.Length == 0)
            {
                yield return new ValidationResult("The file is empty.", new[] { nameof(document) });
            }
            const long maxFileSize = 5 * 1024 * 1024; // 5 MB
            if (document.Length > maxFileSize)
            {
                yield return new ValidationResult($"The file size cannot exceed {maxFileSize / (1024 * 1024)} MB.", new[] { nameof(document) });
            }
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".png", ".jpg", ".jpeg", ".gif", ".bmp" };
            var fileExtension = System.IO.Path.GetExtension(document.FileName).ToLowerInvariant();
            if (!string.IsNullOrEmpty(fileExtension) && !allowedExtensions.Contains(fileExtension))
            {
                yield return new ValidationResult("The file type is not allowed. Allowed types are: .pdf, .doc, .docx, .xls, .xlsx, .ppt, .pptx, .txt, .png, .jpg, .jpeg, .gif, .bmp.", new[] { nameof(document) });
            }
        }
    }
}
public record UpdateCommandDocumentDto : IValidatableObject
{
    [MaxLength(50, ErrorMessage = "name_command_document cannot exceed 50 characters")]
    public string? name_command_document { get; init; }

    public IFormFile? document { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (name_command_document is not null && string.IsNullOrWhiteSpace(name_command_document))
        {
            yield return new ValidationResult("name_command_document cannot be empty or whitespace.", new[] { nameof(name_command_document) });
        }
        if (document is not null)
        {
            if (document.Length == 0)
            {
                yield return new ValidationResult("The file is empty.", new[] { nameof(document) });
            }
            const long maxFileSize = 5 * 1024 * 1024; // 5 MB
            if (document.Length > maxFileSize)
            {
                yield return new ValidationResult($"The file size cannot exceed {maxFileSize / (1024 * 1024)} MB.", new[] { nameof(document) });
            }
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".png", ".jpg", ".jpeg", ".gif", ".bmp" };
            var fileExtension = System.IO.Path.GetExtension(document.FileName).ToLowerInvariant();
            if (!string.IsNullOrEmpty(fileExtension) && !allowedExtensions.Contains(fileExtension))
            {
                yield return new ValidationResult("The file type is not allowed. Allowed types are: .pdf, .doc, .docx, .xls, .xlsx, .ppt, .pptx, .txt, .png, .jpg, .jpeg, .gif, .bmp.", new[] { nameof(document) });
            }
        }
    }
}