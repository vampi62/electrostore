using ElectrostoreAPI.Dto;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata;

namespace ElectrostoreAPI.Models;

public class UserPushSubscriptions : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_push_subscription { get; set; }

    public int id_user { get; set; }
    [ForeignKey("id_user")]
    public Users? User { get; set; }

    [MaxLength(Constants.MaxUrlLength)]
    public required string endpoint { get; set; }

    [MaxLength(Constants.MaxPushKeyLength)]
    public required string p256dh { get; set; }

    [MaxLength(Constants.MaxPushAuthLength)]
    public required string auth { get; set; }

    [MaxLength(Constants.MaxDeviceNameLength)]
    public string? device_name { get; set; }
}
