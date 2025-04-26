using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadCommandCommentaireDto
{
    public int id_command_commentaire { get; init; }
    public int id_command { get; init; }
    public int? id_user { get; init; }
    public string contenu_command_commentaire { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedCommandCommentaireDto : ReadCommandCommentaireDto
{
    public ReadCommandDto? command { get; init; }
    public ReadUserDto? user { get; init; }
}
public record CreateCommandCommentaireByCommandDto : IValidatableObject
{
    [Required]
    [MinLength(1, ErrorMessage = "contenu_command_commentaire cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxCommentaireLength, ErrorMessage = "contenu_command_commentaire cannot exceed 455 characters")]
    public string contenu_command_commentaire { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(contenu_command_commentaire))
        {
            yield return new ValidationResult("contenu_command_commentaire cannot be null, empty, or whitespace.", new[] { nameof(contenu_command_commentaire) });
        }
    }
}
public record CreateCommandCommentaireByUserDto : IValidatableObject
{
    [Required]
    public int id_command { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "contenu_command_commentaire cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxCommentaireLength, ErrorMessage = "contenu_command_commentaire cannot exceed 455 characters")]
    public string contenu_command_commentaire { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(contenu_command_commentaire))
        {
            yield return new ValidationResult("contenu_command_commentaire cannot be null, empty, or whitespace.", new[] { nameof(contenu_command_commentaire) });
        }
    }
}
public record CreateCommandCommentaireDto : IValidatableObject
{
    [Required]
    public int id_command { get; init; }

    [Required]
    public int id_user { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "contenu_command_commentaire cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxCommentaireLength, ErrorMessage = "contenu_command_commentaire cannot exceed 455 characters")]
    public string contenu_command_commentaire { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(contenu_command_commentaire))
        {
            yield return new ValidationResult("contenu_command_commentaire cannot be null, empty, or whitespace.", new[] { nameof(contenu_command_commentaire) });
        }
    }
}
public record UpdateCommandCommentaireDto : IValidatableObject
{
    [MaxLength(Constants.MaxCommentaireLength, ErrorMessage = "contenu_command_commentaire cannot exceed 455 characters")]
    public string? contenu_command_commentaire { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (contenu_command_commentaire is not null && string.IsNullOrWhiteSpace(contenu_command_commentaire))
        {
            yield return new ValidationResult("contenu_command_commentaire cannot be empty or whitespace.", new[] { nameof(contenu_command_commentaire) });
        }
    }
}