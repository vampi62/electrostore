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

    [MaxLength(50)]
    public string nom_item { get; set; }

    public int seuil_min_item { get; set; }

    [MaxLength(500)]
    public string description_item { get; set; }

    public ICollection<ItemsBoxs> ItemsBoxs { get; set; } = new List<ItemsBoxs>();
    public ICollection<ItemsTags> ItemsTags { get; set; } = new List<ItemsTags>();
    public ICollection<ProjetsItems> ProjetsItems { get; set; } = new List<ProjetsItems>();
    public ICollection<CommandsItems> CommandsItems { get; set; } = new List<CommandsItems>();
    public ICollection<ItemsDocuments> ItemsDocuments { get; set; } = new List<ItemsDocuments>();
}