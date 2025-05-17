using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using electrostore.Dto;

namespace electrostore.Models;

public class CommandsCommentaires : BaseEntity
{
    [Key]
    public int id_command_commentaire { get; set; }
    
    public int? id_user { get; set; }
    [ForeignKey("id_user")]
    public Users? User { get; set; }

    public int id_command { get; set; }
    [ForeignKey("id_command")]
    public Commands Command { get; set; }

    [MaxLength(Constants.MaxCommentaireLength)]
    public string contenu_command_commentaire { get; set; }
}