using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace electrostore.Models;

public class Projets
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_projet { get; set; }
    
    [MaxLength(50)]
    public string nom_projet { get; set; }

    [MaxLength(500)]
    public string description_projet { get; set; }

    [MaxLength(150)]
    public string url_projet { get; set; }

    [MaxLength(50)]
    public string status_projet { get; set; }

    public DateTime date_debut_projet { get; set; }

    public DateTime? date_fin_projet { get; set; }

    public ICollection<ProjetsItems> ProjetsItems { get; set; } = new List<ProjetsItems>();
    public ICollection<ProjetsCommentaires> ProjetsCommentaires { get; set; } = new List<ProjetsCommentaires>();
    public ICollection<ProjetsDocuments> ProjetsDocuments { get; set; } = new List<ProjetsDocuments>();
}