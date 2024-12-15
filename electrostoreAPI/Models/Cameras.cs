using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace electrostore.Models;

public class Cameras
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_camera { get; set; }

    [MaxLength(50)]
    public string nom_camera { get; set; }

    [MaxLength(150)]
    public string url_camera { get; set; }

    [MaxLength(50)]
    public string? user_camera { get; set; }

    [MaxLength(50)]
    public string? mdp_camera { get; set; }
}