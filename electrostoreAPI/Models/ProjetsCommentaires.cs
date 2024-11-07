using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace electrostore.Models;

public class ProjetsCommentaires
{
    [Key]
    public int id_projet_commentaire { get; set; }
    
    public int? id_user { get; set; }
    [ForeignKey("id_user")]
    public Users User { get; set; }

    public int id_projet { get; set; }
    [ForeignKey("id_projet")]
    public Projets Projet { get; set; }

    public string contenu_projet_commentaire { get; set; }
    public DateTime date_projet_commentaire { get; set; }
    public DateTime date_modif_projet_commentaire { get; set; }
}