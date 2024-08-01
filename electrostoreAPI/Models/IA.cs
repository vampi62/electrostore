using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace electrostore.Models;

public class IA
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_ia { get; set; }
    
    public string nom_ia { get; set; }
    public string description_ia { get; set; }
    public DateTime date_ia { get; set; }
}