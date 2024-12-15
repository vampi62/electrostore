using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace electrostore.Models;

public class CommandsCommentaires
{
    [Key]
    public int id_command_commentaire { get; set; }
    
    public int? id_user { get; set; }
    [ForeignKey("id_user")]
    public Users User { get; set; }

    public int id_command { get; set; }
    [ForeignKey("id_command")]
    public Commands Command { get; set; }

    [MaxLength(455)]
    public string contenu_command_commentaire { get; set; }

    public DateTime date_command_commentaire { get; set; }

    public DateTime date_modif_command_commentaire { get; set; }

}