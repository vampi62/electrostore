using System.ComponentModel.DataAnnotations;
using electrostore.Validators;

namespace electrostore.Dto;

public record ReadCommandDocumentDto
{
    public int id_command_document { get; init; }
    public int id_command { get; init; }
    public required string url_command_document { get; init; }
    public required string name_command_document { get; init; }
    public required string type_command_document { get; init; }
    public decimal size_command_document { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record CreateCommandDocumentDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_command { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string name_command_document { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [FileSize(Constants.MaxDocumentSizeMB, ErrorMessage = "{0} cannot exceed {1} MB in size.")]
    [FileType(nameof(MimeTypes.AllowedDocumentMimeTypes),
        ErrorMessage = "{0} has an invalid file type, allowed types are: [{1}].")]
    public required IFormFile document { get; init; }
}
public record CreateCommandDocumentByCommandDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string name_command_document { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [FileSize(Constants.MaxDocumentSizeMB, ErrorMessage = "{0} cannot exceed {1} MB in size.")]
    [FileType(nameof(MimeTypes.AllowedDocumentMimeTypes),
        ErrorMessage = "{0} has an invalid file type, allowed types are: [{1}].")]
    public required IFormFile document { get; init; }
}
public record UpdateCommandDocumentDto
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? name_command_document { get; init; }
}