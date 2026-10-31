using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Constants;
using ElectrostoreAPI.Enums;

namespace ElectrostoreAPI.Models;

public class UsersSessions : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid id_user_session { get; set; }

    public int id_user { get; set; }
    [ForeignKey("id_user")]
    public Users? User { get; set; }

    public required string auth_method { get; set; }

    public int? id_user_external_login { get; set; }
    [ForeignKey("id_user_external_login")]
    public UsersExternalLogins? UserExternalLogin { get; set; }

    [MaxLength(FieldLengths.MaxIpLength)]
    public required string created_by_ip { get; set; }

    public DateTime? revoked_at { get; set; }

    [MaxLength(FieldLengths.MaxIpLength)]
    public string? revoked_by_ip { get; set; }

    public ICollection<JwtTokens> JwtTokens { get; set; } = new List<JwtTokens>();
}