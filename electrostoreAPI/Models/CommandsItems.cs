using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace electrostore.Models;

public class CommandsItems : BaseEntity
{
    public int id_item { get; set; }
    [ForeignKey("id_item")]
    public Items Item { get; set; }

    public int id_command { get; set; }
    [ForeignKey("id_command")]
    public Commands Command { get; set; }

    public int qte_command_item { get; set; }

    public float prix_command_item { get; set; }
}