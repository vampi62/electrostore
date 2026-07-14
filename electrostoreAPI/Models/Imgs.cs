using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Models;

public class Imgs : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_img { get; set; }

    public int id_item { get; set; }
    [ForeignKey("id_item")]
    public Items? Item { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public required string nom_img { get; set; }

    [MaxLength(Constants.MaxUrlFileLength)]
    public required string url_picture_img { get; set; }

    [MaxLength(Constants.MaxUrlFileLength)]
    public required string url_thumbnail_img { get; set; }

    [MaxLength(Constants.MaxDescriptionLength)]
    public string description_img { get; set; } = string.Empty;
}