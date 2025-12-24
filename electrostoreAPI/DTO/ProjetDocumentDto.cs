using System.ComponentModel.DataAnnotations;
using electrostore.Validators;

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
public record CreateProjetDocumentDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_projet { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string name_projet_document { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [FileSize(Constants.MaxDocumentSizeMB, ErrorMessage = "{0} cannot exceed {1} MB in size.")]
    [FileType(nameof(Constants.AllowedDocumentMimeTypes),
        ErrorMessage = "{0} has an invalid file type, allowed types are: [{1}], and extensions are: [{2}].")]
    public required IFormFile document { get; init; }
}
public record CreateProjetDocumentByProjetDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string name_projet_document { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [FileSize(Constants.MaxDocumentSizeMB, ErrorMessage = "{0} cannot exceed {1} MB in size.")]
    [FileType(nameof(Constants.AllowedDocumentMimeTypes),
        ErrorMessage = "{0} has an invalid file type, allowed types are: [{1}], and extensions are: [{2}].")]
    public required IFormFile document { get; init; }
}
public record UpdateProjetDocumentDto
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? name_projet_document { get; init; }
}