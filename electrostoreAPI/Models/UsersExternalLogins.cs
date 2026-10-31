using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Constants;
using ElectrostoreAPI.Enums;

namespace ElectrostoreAPI.Models;

public class UsersExternalLogins : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_user_external_login { get; set; }

    public int id_user { get; set; }
    [ForeignKey("id_user")]
    public Users? User { get; set; }

    public required string provider { get; set; }

    public required string provider_subject { get; set; }

    public ICollection<UsersSessions> UsersSessions { get; set; } = new List<UsersSessions>();
}