using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using electrostore.Dto;

namespace electrostore.Models;

public class Users
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_user { get; set; }
    
    [MaxLength(Constants.MaxNameLength)]
    public string nom_user { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public string prenom_user { get; set; }

    [EmailAddress]
    [MaxLength(Constants.MaxEmailLength)]
    public string email_user { get; set; }

    [MaxLength(255)]
    public string mdp_user { get; set; }

    [MaxLength(Constants.MaxRoleLength)]
    public string role_user { get; set; }

    public Guid? reset_token { get; set; }

    public DateTime? reset_token_expiration { get; set; }

    public ICollection<ProjetsCommentaires> ProjetsCommentaires { get; set; } = new List<ProjetsCommentaires>();
    public ICollection<CommandsCommentaires> CommandsCommentaires { get; set; } = new List<CommandsCommentaires>();
}