using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using electrostore.Dto;

namespace electrostore.Models;

public class Stores
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_store { get; set; }
    
    [MaxLength(Constants.MaxNameLength)]
    public string nom_store { get; set; }

    public int xlength_store { get; set; }

    public int ylength_store { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public string mqtt_name_store { get; set; }

    public ICollection<Boxs> Boxs { get; set; } = new List<Boxs>();
    public ICollection<Leds> Leds { get; set; } = new List<Leds>();
    public ICollection<StoresTags> StoresTags { get; set; } = new List<StoresTags>();
}