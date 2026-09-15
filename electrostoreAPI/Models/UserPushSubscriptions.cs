using ElectrostoreAPI.Constants;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace ElectrostoreAPI.Models;

public class UserPushSubscriptions : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_user_push_subscription { get; set; }

    public int id_user { get; set; }
    [ForeignKey("id_user")]
    public Users? User { get; set; }

    [MaxLength(FieldLengths.MaxUrlLength)]
    public required string endpoint { get; set; }

    [MaxLength(FieldLengths.MaxPushKeyLength)]
    public required string p256dh { get; set; }

    [MaxLength(FieldLengths.MaxPushAuthLength)]
    public required string auth { get; set; }

    [MaxLength(FieldLengths.MaxDeviceNameLength)]
    public string? device_name { get; set; }
}
