using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace electrostore.Models;

public class CommandsDocuments
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_command_document { get; set; }

    public string url_command_document { get; set; }

    public string name_command_document { get; set; }

    public string type_command_document { get; set; }

    public decimal size_command_document { get; set; }

    public DateTime date_command_document { get; set; }

    public int id_command { get; set; }
    [ForeignKey("id_command")]
    public Commands Command { get; set; }
}