using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record ReadProjetCommentaireDto
{
    public int id_projetcommentaire { get; init; }
    public int id_projet { get; init; }
    public int? id_user { get; init; }
    public string contenu_projetcommentaire { get; init; }
    public DateTime date_projetcommentaire { get; init; }
    public DateTime date_modif_projetcommentaire { get; init; }
    public string? user_name { get; init; }
    public ReadProjetDto? projet { get; init; }
}
public record CreateProjetCommentaireByUserDto
{
    [Required] public int id_projet { get; init; }
    [Required] public string contenu_projetcommentaire { get; init; }
}
public record CreateProjetCommentaireByProjetDto
{
    [Required] public string contenu_projetcommentaire { get; init; }
}
public record CreateProjetCommentaireDto
{
    [Required] public int id_projet { get; init; }
    [Required] public int id_user { get; init; }
    [Required] public string contenu_projetcommentaire { get; init; }
}
public record UpdateProjetCommentaireDto
{
    public string? contenu_projetcommentaire { get; init; }
}