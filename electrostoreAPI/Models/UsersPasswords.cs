using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Constants;
using ElectrostoreAPI.Enums;

namespace ElectrostoreAPI.Models;

public class UsersPasswords : BaseEntity
{
    [Key]
    public int id_user { get; set; }
    [ForeignKey("id_user")]
    public Users? User { get; set; }

    [MaxLength(255)]
    public required string password_user { get; set; }

    public DateTime? password_last_changed { get; set; }

    public Guid? reset_token { get; set; }

    public DateTime? reset_token_expiration { get; set; }

    public DateTime? reset_token_sent_at { get; set; }
}