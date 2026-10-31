using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Constants;
using ElectrostoreAPI.Enums;

namespace ElectrostoreAPI.Models;

public class Users : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_user { get; set; }

    [MaxLength(FieldLengths.MaxNameLength)]
    public required string name_user { get; set; }

    [MaxLength(FieldLengths.MaxNameLength)]
    public required string firstname_user { get; set; }

    [EmailAddress]
    [MaxLength(FieldLengths.MaxEmailLength)]
    public required string email_user { get; set; }

    public bool is_active { get; set; } = true;

    public bool is_verified { get; set; } = false;

    public DateTime? last_login { get; set; }

    public Guid? email_verification_token { get; set; }

    public DateTime? email_verification_token_expiration { get; set; }

    public DateTime? email_verification_token_sent_at { get; set; }

    public UserRole role_user { get; set; } = UserRole.User;

    public ICollection<CommandsComments> CommandsComments { get; set; } = new List<CommandsComments>();
    public ICollection<ProjectsComments> ProjectsComments { get; set; } = new List<ProjectsComments>();
    public ICollection<UsersExternalLogins> UsersExternalLogins { get; set; } = new List<UsersExternalLogins>();
    public ICollection<UsersSessions> UsersSessions { get; set; } = new List<UsersSessions>();
    public ICollection<UsersPasswords> UsersPasswords { get; set; } = new List<UsersPasswords>();
}