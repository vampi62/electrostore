using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectrostoreAPI.Models;

public class UserPushSubscriptions : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_push_subscription { get; set; }

    public int id_user { get; set; }
    [ForeignKey("id_user")]
    public Users? User { get; set; }

    [MaxLength(2048)]
    public required string endpoint { get; set; }

    [MaxLength(512)]
    public required string p256dh { get; set; }

    [MaxLength(256)]
    public required string auth { get; set; }

    [MaxLength(255)]
    public string? device_name { get; set; }
}
