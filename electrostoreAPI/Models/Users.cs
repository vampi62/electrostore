using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;

namespace ElectrostoreAPI.Models;

public class Users : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_user { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public required string name_user { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public required string firstname_user { get; set; }

    [EmailAddress]
    [MaxLength(Constants.MaxEmailLength)]
    public required string email_user { get; set; }

    [MaxLength(255)]
    public required string password_user { get; set; }

    public UserRole role_user { get; set; } = UserRole.User;

    public Guid? reset_token { get; set; }

    public DateTime? reset_token_expiration { get; set; }

    public ICollection<CommandsCommentaires> CommandsCommentaires { get; set; } = new List<CommandsCommentaires>();
    public ICollection<ProjetsCommentaires> ProjetsCommentaires { get; set; } = new List<ProjetsCommentaires>();
}