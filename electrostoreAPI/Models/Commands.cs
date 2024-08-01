using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace electrostore.Models;

public class Commands
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_command { get; set; }

    public float prix_command { get; set; }
    public string url_command { get; set; }
    public string status_command { get; set; }
    public DateTime date_command { get; set; }
    public DateTime? date_livraison_command { get; set; }
}