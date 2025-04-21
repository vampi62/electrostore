using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace electrostore.Models;

public class ProjetsItems : BaseEntity
{
    public int id_projet { get; set; }
    [ForeignKey("id_projet")]
    public Projets Projet { get; set; }
    
    public int id_item { get; set; }
    [ForeignKey("id_item")]
    public Items Item { get; set; }

    public int qte_projet_item { get; set; }
}