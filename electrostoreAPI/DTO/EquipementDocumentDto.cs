using ElectrostoreAPI.Validators;
using System.ComponentModel.DataAnnotations;

namespace ElectrostoreAPI.Dto;

public record ReadEquipementDocumentDto
{
    public int id_equipement_document { get; init; }
    public int id_equipement { get; init; }
    public required string url_equipement_document { get; init; }
    public required string name_equipement_document { get; init; }
    public required string type_equipement_document { get; init; }
    public decimal size_equipement_document { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record CreateEquipementDocumentDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_equipement { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string name_equipement_document { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [FileSize(nameof(Constants.MaxDocumentSizeMB), ErrorMessage = "{0} cannot exceed {1} MB in size.")]
    [FileType(nameof(Constants.AllowedDocumentMimeTypes),
        ErrorMessage = "{0} has an invalid file type, allowed types are: [{1}], and extensions are: [{2}].")]
    public required IFormFile document { get; init; }
}
public record CreateEquipementDocumentByEquipementDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string name_equipement_document { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [FileSize(nameof(Constants.MaxDocumentSizeMB), ErrorMessage = "{0} cannot exceed {1} MB in size.")]
    [FileType(nameof(Constants.AllowedDocumentMimeTypes),
        ErrorMessage = "{0} has an invalid file type, allowed types are: [{1}], and extensions are: [{2}].")]
    public required IFormFile document { get; init; }
}
public record UpdateEquipementDocumentDto
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? name_equipement_document { get; init; }
}
