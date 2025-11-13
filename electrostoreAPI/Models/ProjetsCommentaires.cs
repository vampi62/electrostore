using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using electrostore.Dto;

namespace electrostore.Models;

public class ProjetsCommentaires : BaseEntity
{
    [Key]
    public int id_projet_commentaire { get; set; }

    public int? id_user { get; set; }
    [ForeignKey("id_user")]
    public Users? User { get; set; }

    public int id_projet { get; set; }
    [ForeignKey("id_projet")]
    public Projets? Projet { get; set; }

    [MaxLength(Constants.MaxCommentaireLength)]
    public required string contenu_projet_commentaire { get; set; }
}