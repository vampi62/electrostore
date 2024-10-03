using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace electrostore.Models;

public class Items
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_item { get; set; }
    
    public int? id_img { get; set; }
    [ForeignKey("id_img")]
    public Imgs Img { get; set; }

    public string nom_item { get; set; }
    public int seuil_min_item { get; set; }
    public string datasheet_item { get; set; }
    public string description_item { get; set; }
    public ICollection<ItemsBoxs> ItemsBoxs { get; set; } = new List<ItemsBoxs>();
}