using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Models;

public class Cameras : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_camera { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public required string nom_camera { get; set; }

    [MaxLength(Constants.MaxUrlLength)]
    public required string url_camera { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public string? user_camera { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public string? mdp_camera { get; set; }

    public DateTime? last_seen_camera { get; set; }
}