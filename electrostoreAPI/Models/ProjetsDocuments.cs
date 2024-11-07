using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace electrostore.Models;

public class ProjetsDocuments
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_projet_document { get; set; }

    public string url_projet_document { get; set; }

    public string name_projet_document { get; set; }

    public string type_projet_document { get; set; }

    public decimal size_projet_document { get; set; }

    public DateTime date_projet_document { get; set; }

    public int id_projet { get; set; }
    [ForeignKey("id_projet")]
    public Projets Projet { get; set; }
}