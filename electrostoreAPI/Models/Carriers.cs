using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

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
    [MaxLength(Constants.MaxEmailLength)]
    public string? email_carrier { get; set; }

    [Phone]
    public string? tel_carrier { get; set; }

    [MaxLength(Constants.MaxUrlLength)]
    public string? url_carrier { get; set; }

    public string? name_carrier { get; set; }
}