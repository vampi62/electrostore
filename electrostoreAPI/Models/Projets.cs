using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace electrostore.Models;

public class Projets
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_projet { get; set; }
    
    public string nom_projet { get; set; }
    public string description_projet { get; set; }
    public string url_projet { get; set; }
    public string status_projet { get; set; }
    public DateTime date_projet { get; set; }
    public DateTime? date_fin_projet { get; set; }
}