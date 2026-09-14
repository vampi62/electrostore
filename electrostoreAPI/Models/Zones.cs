using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Constants;

namespace ElectrostoreAPI.Models;

public class Zones : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_zone { get; set; }

    [MaxLength(FieldLengths.MaxNameLength)]
    public required string name_zone { get; set; }

    [MaxLength(FieldLengths.MaxDescriptionLength)]
    public string description_zone { get; set; } = string.Empty;

    public int xlength_zone { get; set; }

    public int ylength_zone { get; set; }

    [MaxLength(FieldLengths.MaxUrlFileLength)]
    public string? url_picture_zone { get; set; }

    [MaxLength(FieldLengths.MaxUrlFileLength)]
    public string? url_thumbnail_zone { get; set; }

    public ICollection<Stores> Stores { get; set; } = new List<Stores>();
}
