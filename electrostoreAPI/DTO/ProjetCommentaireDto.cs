using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadProjetCommentaireDto
{
    public int id_projet_commentaire { get; init; }
    public int id_projet { get; init; }
    public int? id_user { get; init; }
    public required string contenu_projet_commentaire { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedProjetCommentaireDto : ReadProjetCommentaireDto
{
    public ReadProjetDto? projet { get; init; }
    public ReadUserDto? user { get; init; }
}
public record CreateProjetCommentaireByUserDto
{
    [Required]
    public required int id_projet { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "contenu_projet_commentaire cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxCommentaireLength, ErrorMessage = "contenu_projet_commentaire cannot exceed 455 characters")]
    public required string contenu_projet_commentaire { get; init; }
}
public record CreateProjetCommentaireByProjetDto
{
    [Required]
    [MinLength(1, ErrorMessage = "contenu_projet_commentaire cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxCommentaireLength, ErrorMessage = "contenu_projet_commentaire cannot exceed 455 characters")]
    public required string contenu_projet_commentaire { get; init; }
}
public record CreateProjetCommentaireDto
{
    [Required]
    public required int id_projet { get; init; }

    [Required]
    public required int id_user { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "contenu_projet_commentaire cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxCommentaireLength, ErrorMessage = "contenu_projet_commentaire cannot exceed 455 characters")]
    public required string contenu_projet_commentaire { get; init; }
}
public record UpdateProjetCommentaireDto : IValidatableObject
{
    [MaxLength(Constants.MaxCommentaireLength, ErrorMessage = "contenu_projet_commentaire cannot exceed 455 characters")]
    public string? contenu_projet_commentaire { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (contenu_projet_commentaire is not null && string.IsNullOrWhiteSpace(contenu_projet_commentaire))
        {
            yield return new ValidationResult("contenu_projet_commentaire cannot be empty or whitespace.", new[] { nameof(contenu_projet_commentaire) });
        }
    }
}