using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace electrostore.Models;

public class ProjetsProjetTags : BaseEntity
{
    public int id_projet_tag { get; set; }
    [ForeignKey("id_projet_tag")]
    public ProjetTags? ProjetTag { get; set; }

    public int id_projet { get; set; }
    [ForeignKey("id_projet")]
    public Projets? Projet { get; set; }
}