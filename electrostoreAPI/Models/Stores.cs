using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace electrostore.Models;

public class Stores
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_store { get; set; }
    
    public string nom_store { get; set; }
    public int xlength_store { get; set; }
    public int ylength_store { get; set; }
    public string mqtt_name_store { get; set; }
}