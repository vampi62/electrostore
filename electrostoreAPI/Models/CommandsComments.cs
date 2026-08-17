using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Models;

public class CommandsComments : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_command_comment { get; set; }

    public int? id_user { get; set; }
    [ForeignKey("id_user")]
    public Users? User { get; set; }

    public int id_command { get; set; }
    [ForeignKey("id_command")]
    public Commands? Command { get; set; }

    [MaxLength(Constants.MaxCommentLength)]
    public required string content_command_comment { get; set; }
}