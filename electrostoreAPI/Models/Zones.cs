using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Models;

public class Zones : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_zone { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public required string name_zone { get; set; }

    [MaxLength(Constants.MaxDescriptionLength)]
    public string description_zone { get; set; } = string.Empty;

    public int xlength_zone { get; set; }

    public int ylength_zone { get; set; }

    [MaxLength(Constants.MaxUrlFileLength)]
    public string? url_picture_zone { get; set; }

    [MaxLength(Constants.MaxUrlFileLength)]
    public string? url_thumbnail_zone { get; set; }

    public ICollection<Stores> Stores { get; set; } = new List<Stores>();
}
