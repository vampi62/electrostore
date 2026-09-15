using ElectrostoreAPI.Constants;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectrostoreAPI.Models;

public class Carriers : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_carrier { get; set; }

    public int key_carrier { get; set; }

    public int? country_carrier { get; set; }

    public string? country_iso_carrier { get; set; }

    [EmailAddress]
    [MaxLength(FieldLengths.MaxEmailLength)]
    public string? email_carrier { get; set; }

    [Phone]
    public string? tel_carrier { get; set; }

    [MaxLength(FieldLengths.MaxUrlLength)]
    public string? url_carrier { get; set; }

    public string? name_carrier { get; set; }
}