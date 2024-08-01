using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace electrostore.Models;

public class CommandsCommentaires
{
    [Key]
    public int id_commandcommentaire { get; set; }
    
    public int id_user { get; set; }
    [ForeignKey("id_user")]
    public Users User { get; set; }

    public int id_command { get; set; }
    [ForeignKey("id_command")]
    public Commands Command { get; set; }

    public string contenu_commandcommentaire { get; set; }
    public DateTime date_commandcommentaire { get; set; }
    public DateTime date_modif_projetcommentaire { get; set; }

}