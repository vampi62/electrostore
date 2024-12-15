using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace electrostore.Models;

public class IA
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_ia { get; set; }
    
    [MaxLength(50)]
    public string nom_ia { get; set; }

    [MaxLength(500)]
    public string description_ia { get; set; }

    public DateTime date_ia { get; set; }

    public bool trained_ia { get; set; } = false;
}