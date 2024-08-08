using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace electrostore.Models;

public class ProjetsCommentaires
{
    [Key]
    public int id_projetcommentaire { get; set; }
    
    public int? id_user { get; set; }
    [ForeignKey("id_user")]
    public Users User { get; set; }

    public int id_projet { get; set; }
    [ForeignKey("id_projet")]
    public Projets Projet { get; set; }

    public string contenu_projetcommentaire { get; set; }
    public DateTime date_projetcommentaire { get; set; }
    public DateTime date_modif_projetcommentaire { get; set; }
}