using System.ComponentModel.DataAnnotations;
using electrostore.Validators;

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
    [Required(ErrorMessage = "{0} is required.")]
    public int id_projet { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxCommentaireLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? contenu_projet_commentaire { get; init; }
}
public record CreateProjetCommentaireByProjetDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxCommentaireLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? contenu_projet_commentaire { get; init; }
}
public record CreateProjetCommentaireDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public int id_projet { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    public int id_user { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxCommentaireLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? contenu_projet_commentaire { get; init; }
}
public record UpdateProjetCommentaireDto
{
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    [MaxLength(Constants.MaxCommentaireLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public string? contenu_projet_commentaire { get; init; }
}