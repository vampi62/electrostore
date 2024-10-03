using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace electrostore.Models;

public class Users
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_user { get; set; }
    
    public string nom_user { get; set; }
    public string prenom_user { get; set; }
    public string email_user { get; set; }
    public string mdp_user { get; set; }
    public string role_user { get; set; }
    public string? reset_token { get; set; }
    public DateTime? reset_token_expiration { get; set; }
    public ICollection<ProjetsCommentaires> ProjetsCommentaires { get; set; } = new List<ProjetsCommentaires>();
    public ICollection<CommandsCommentaires> CommandsCommentaires { get; set; } = new List<CommandsCommentaires>();
}