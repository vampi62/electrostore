using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadIADto
{
    public int id_ia { get; init; }
    public string nom_ia { get; init; }
    public string description_ia { get; init; }
    public DateTime date_ia { get; init; }
    public bool trained_ia { get; init; }
}
public record CreateIADto : IValidatableObject
{
    [Required]
    [MinLength(1, ErrorMessage = "nom_store cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "nom_store cannot exceed 50 characters")]
    public string nom_ia { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "nom_store cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "nom_store cannot exceed 500 characters")]
    public string description_ia { get; init; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(nom_ia))
        {
            yield return new ValidationResult("nom_ia cannot be null, empty, or whitespace.", new[] { nameof(nom_ia) });
        }
        if (string.IsNullOrWhiteSpace(description_ia))
        {
            yield return new ValidationResult("description_ia cannot be null, empty, or whitespace.", new[] { nameof(description_ia) });
        }
    }
}
public record UpdateIADto : IValidatableObject
{
    [MaxLength(Constants.MaxNameLength, ErrorMessage = "nom_store cannot exceed 50 characters")]
    public string? nom_ia { get; init; }

    [MaxLength(Constants.MaxDescriptionLength, ErrorMessage = "nom_store cannot exceed 500 characters")]
    public string? description_ia { get; init; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (nom_ia is not null && string.IsNullOrWhiteSpace(nom_ia))
        {
            yield return new ValidationResult("nom_ia cannot be null, empty, or whitespace.", new[] { nameof(nom_ia) });
        }
        if (description_ia is not null && string.IsNullOrWhiteSpace(description_ia))
        {
            yield return new ValidationResult("description_ia cannot be null, empty, or whitespace.", new[] { nameof(description_ia) });
        }
    }
}

public class TrainingStatus
{
    public int Progress { get; set; }
    public bool IsCompleted { get; set; }
    public string? Message { get; set; }
    public bool IsRunning { get; set; }
}

public class GetTrainStart
{
    public bool TrainStarted { get; set; }
    public string msg { get; set; }
}

public record DetecDto : IValidatableObject
{
    [Required]
    public IFormFile img_file { get; init; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (img_file is not null)
        {
            const long maxFileSize = Constants.MaxDocumentSizeMB * 1024 * 1024;
            if (img_file.Length > maxFileSize)
            {
                yield return new ValidationResult($"The file size cannot exceed {Constants.MaxDocumentSizeMB} MB.", new[] { nameof(img_file) });
            }
            var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp" };
            var fileExtension = System.IO.Path.GetExtension(img_file.FileName).ToLowerInvariant();
            if (!string.IsNullOrEmpty(fileExtension) && !allowedExtensions.Contains(fileExtension))
            {
                yield return new ValidationResult("The file type is not allowed. Allowed types are: .png, .jpg, .jpeg, .gif, .bmp.", new[] { nameof(img_file) });
            }
        }
    }
}
public record PredictionOutput
{
    public int PredictedLabel { get; init; }
    public float Score { get; init; }
}


/* 
public class TrainImageData
{
    public int id_img { get; set; }
    public string url_img { get; set; }
    public int id_item { get; set; }
}

public class PredictionInput
{
    public int id_img { get; set; }
    public string url_img { get; set; }
    public int id_item { get; set; }
}
public class PredictionOutput
{
    public int PredictedLabel { get; set; }
    public float[] Score { get; set; }
} */