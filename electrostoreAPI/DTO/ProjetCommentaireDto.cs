using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadProjetCommentaireDto
{
    public int id_projet_commentaire { get; init; }
    public int id_projet { get; init; }
    public int? id_user { get; init; }
    public string contenu_projet_commentaire { get; init; }
    public DateTime date_projet_commentaire { get; init; }
    public DateTime date_modif_projet_commentaire { get; init; }
}
public record ReadExtendedProjetCommentaireDto
{
    public int id_projet_commentaire { get; init; }
    public int id_projet { get; init; }
    public int? id_user { get; init; }
    public string contenu_projet_commentaire { get; init; }
    public DateTime date_projet_commentaire { get; init; }
    public DateTime date_modif_projet_commentaire { get; init; }
    public ReadProjetDto? projet { get; init; }
    public ReadUserDto? user { get; init; }
}
public record CreateProjetCommentaireByUserDto : IValidatableObject
{
    [Required]
    public int id_projet { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "contenu_projet_commentaire cannot be empty or whitespace.")]
    [MaxLength(455, ErrorMessage = "contenu_projet_commentaire cannot exceed 455 characters")]
    public string contenu_projet_commentaire { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(contenu_projet_commentaire))
        {
            yield return new ValidationResult("contenu_projet_commentaire cannot be null, empty, or whitespace.", new[] { nameof(contenu_projet_commentaire) });
        }
    }
}
public record CreateProjetCommentaireByProjetDto : IValidatableObject
{
    [Required]
    [MinLength(1, ErrorMessage = "contenu_projet_commentaire cannot be empty or whitespace.")]
    [MaxLength(455, ErrorMessage = "contenu_projet_commentaire cannot exceed 455 characters")]
    public string contenu_projet_commentaire { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(contenu_projet_commentaire))
        {
            yield return new ValidationResult("contenu_projet_commentaire cannot be null, empty, or whitespace.", new[] { nameof(contenu_projet_commentaire) });
        }
    }
}
public record CreateProjetCommentaireDto : IValidatableObject
{
    [Required]
    public int id_projet { get; init; }

    [Required]
    public int id_user { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "contenu_projet_commentaire cannot be empty or whitespace.")]
    [MaxLength(455, ErrorMessage = "contenu_projet_commentaire cannot exceed 455 characters")]
    public string contenu_projet_commentaire { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(contenu_projet_commentaire))
        {
            yield return new ValidationResult("contenu_projet_commentaire cannot be null, empty, or whitespace.", new[] { nameof(contenu_projet_commentaire) });
        }
    }
}
public record UpdateProjetCommentaireDto : IValidatableObject
{
    [MaxLength(455, ErrorMessage = "contenu_projet_commentaire cannot exceed 455 characters")]
    public string? contenu_projet_commentaire { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (contenu_projet_commentaire is not null && string.IsNullOrWhiteSpace(contenu_projet_commentaire))
        {
            yield return new ValidationResult("contenu_projet_commentaire cannot be empty or whitespace.", new[] { nameof(contenu_projet_commentaire) });
        }
    }
}