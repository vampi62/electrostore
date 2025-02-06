using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using electrostore.Dto;

namespace electrostore.Models;

public class Cameras
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_camera { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public string nom_camera { get; set; }

    [MaxLength(Constants.MaxUrlLength)]
    public string url_camera { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public string? user_camera { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public string? mdp_camera { get; set; }
}