using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using electrostore.Dto;

namespace electrostore.Models;

public class Imgs : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_img { get; set; }
    
    public int id_item { get; set; }
    [ForeignKey("id_item")]
    public Items Item { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public string nom_img { get; set; }

    [MaxLength(Constants.MaxUrlLength)]
    public string url_img { get; set; }

    [MaxLength(Constants.MaxDescriptionLength)]
    public string description_img { get; set; }
}