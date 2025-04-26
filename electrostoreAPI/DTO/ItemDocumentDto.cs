using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadItemDocumentDto
{
    public int id_item_document { get; init; }
    public int id_item { get; init; }
    public string url_item_document { get; init; }
    public string name_item_document { get; init; }
    public string type_item_document { get; init; }
    public decimal size_item_document { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record CreateItemDocumentDto
{
    [Required]
    public int id_item { get; init; }
    
    [Required]
    [MinLength(1, ErrorMessage = "name_item_document cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "name_item_document cannot exceed 50 characters")]
    public string name_item_document { get; init; }
    
    [Required]
    public IFormFile document { get; init; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(name_item_document))
        {
            yield return new ValidationResult("name_item_document cannot be null, empty, or whitespace.", new[] { nameof(name_item_document) });
        }
        if (document is not null)
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
}
public record CreateItemDocumentByItemDto : IValidatableObject
{
    [Required]
    [MinLength(1, ErrorMessage = "name_item_document cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "name_item_document cannot exceed 50 characters")]
    public string name_item_document { get; init; }
    
    [Required]
    public IFormFile document { get; init; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(name_item_document))
        {
            yield return new ValidationResult("name_item_document cannot be null, empty, or whitespace.", new[] { nameof(name_item_document) });
        }
        if (document is not null)
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
}
public record UpdateItemDocumentDto : IValidatableObject
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "name_item_document cannot exceed 50 characters")]
    public string? name_item_document { get; init; }

    public IFormFile? document { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (name_item_document is not null && string.IsNullOrWhiteSpace(name_item_document))
        {
            yield return new ValidationResult("name_item_document cannot be empty or whitespace.", new[] { nameof(name_item_document) });
        }
        if (document is not null)
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
}