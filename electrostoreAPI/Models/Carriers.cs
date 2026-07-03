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

    public int key { get; set; }

    public int? country { get; set; }

    public string? country_iso { get; set; }

    [EmailAddress]
    [MaxLength(Constants.MaxEmailLength)]
    public string? email { get; set; }

    [Phone]
    public string? tel { get; set; }

    [MaxLength(Constants.MaxUrlLength)]
    public string? url { get; set; }

    public string? name { get; set; }
}