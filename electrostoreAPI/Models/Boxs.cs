using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace electrostore.Models;

public class Boxs : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_box { get; set; }

    public int id_store { get; set; }
    [ForeignKey("id_store")]
    public Stores Store { get; set; }

    public int xstart_box { get; set; }

    public int ystart_box { get; set; }

    public int xend_box { get; set; }

    public int yend_box { get; set; }

    public ICollection<BoxsTags> BoxsTags { get; set; } = new List<BoxsTags>();
    public ICollection<ItemsBoxs> ItemsBoxs { get; set; } = new List<ItemsBoxs>();
}