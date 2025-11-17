using System.ComponentModel.DataAnnotations;
using electrostore.Validators;

namespace electrostore.Dto;

public record ReadCommandCommentaireDto
{
    public int id_command_commentaire { get; init; }
    public int id_command { get; init; }
    public int? id_user { get; init; }
    public required string contenu_command_commentaire { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}
public record ReadExtendedCommandCommentaireDto : ReadCommandCommentaireDto
{
    public ReadCommandDto? command { get; init; }
    public ReadUserDto? user { get; init; }
}
public record CreateCommandCommentaireByCommandDto
{
    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxCommentaireLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string contenu_command_commentaire { get; init; }
}
public record CreateCommandCommentaireByUserDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_command { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxCommentaireLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string contenu_command_commentaire { get; init; }
}
public record CreateCommandCommentaireDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required int id_command { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    public required int id_user { get; init; }

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxCommentaireLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string contenu_command_commentaire { get; init; }
}
public record UpdateCommandCommentaireDto
{
    [MaxLength(Constants.MaxCommentaireLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? contenu_command_commentaire { get; init; }
}