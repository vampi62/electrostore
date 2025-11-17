using System.ComponentModel.DataAnnotations;
using electrostore.Validators;

namespace electrostore.Dto;

public record ReadIADto
{
    public int id_ia { get; init; }
    public required string nom_ia { get; init; }
    public required string description_ia { get; init; }
    public bool trained_ia { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record CreateIADto
{
    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string nom_ia { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string description_ia { get; init; }
}
public record UpdateIADto
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? nom_ia { get; init; }

    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? description_ia { get; init; }
}

public record DetecDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [FileSize(Constants.MaxDocumentSizeMB, ErrorMessage = "{0} cannot exceed {1} MB in size.")]
    [FileExtension([".png", ".jpg", ".jpeg", ".gif", ".bmp"],
        ErrorMessage = "{0} has an invalid file type, allowed types are: [{1}].")]
    public required IFormFile img_file { get; init; }
}
public record PredictionOutput
{
    public int PredictedLabel { get; init; }
    public float Score { get; init; }
}

public record IAStatusDto
{
    public required string Status { get; init; }
    public required string Message { get; init; }
    public int Epoch { get; init; }
    public float Accuracy { get; init; }
    public float ValAccuracy { get; init; }
    public float Loss { get; init; }
    public float ValLoss { get; init; }
}