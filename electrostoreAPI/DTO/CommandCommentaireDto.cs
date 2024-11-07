using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadCommandCommentaireDto
{
    public int id_commandcommentaire { get; init; }
    public int id_command { get; init; }
    public int? id_user { get; init; }
    public string contenu_command_commentaire { get; init; }
    public DateTime date_command_commentaire { get; init; }
    public DateTime date_modif_command_commentaire { get; init; }
    public string? user_name { get; init; }
    public ReadCommandDto? command { get; init; }
}
public record CreateCommandCommentaireByCommandDto
{
    [Required] public string contenu_command_commentaire { get; init; }
}
public record CreateCommandCommentaireByUserDto
{
    [Required] public int id_command { get; init; }
    [Required] public string contenu_command_commentaire { get; init; }
}
public record CreateCommandCommentaireDto
{
    [Required] public int id_command { get; init; }
    [Required] public int id_user { get; init; }
    [Required] public string contenu_command_commentaire { get; init; }
}
public record UpdateCommandCommentaireDto
{
    public string? contenu_command_commentaire { get; init; }
}