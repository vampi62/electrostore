using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace electrostore.Models;

public class Imgs
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_img { get; set; }
    
    public int id_item { get; set; }
    [ForeignKey("id_item")]
    public Items Item { get; set; }

    [MaxLength(50)]
    public string nom_img { get; set; }

    [MaxLength(150)]
    public string url_img { get; set; }

    [MaxLength(500)]
    public string description_img { get; set; }

    public DateTime date_img { get; set; }
}