using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using electrostore.Dto;
using electrostore.Enums;

namespace electrostore.Models;

public class Users : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_user { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public required string nom_user { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public required string prenom_user { get; set; }

    [EmailAddress]
    [MaxLength(Constants.MaxEmailLength)]
    public required string email_user { get; set; }

    [MaxLength(255)]
    public required string mdp_user { get; set; }

    public UserRole role_user { get; set; } = UserRole.User;

    public Guid? reset_token { get; set; }

    public DateTime? reset_token_expiration { get; set; }

    public ICollection<CommandsCommentaires> CommandsCommentaires { get; set; } = new List<CommandsCommentaires>();
    public ICollection<ProjetsCommentaires> ProjetsCommentaires { get; set; } = new List<ProjetsCommentaires>();
}