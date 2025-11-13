using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using electrostore.Dto;
using System.Numerics;

namespace electrostore.Models;

public class CommandsDocuments : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_command_document { get; set; }

    [MaxLength(Constants.MaxUrlLength)]
    public required string url_command_document { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public required string name_command_document { get; set; }

    [MaxLength(Constants.MaxTypeLength)]
    public required string type_command_document { get; set; }

    public decimal size_command_document { get; set; }

    public int id_command { get; set; }
    [ForeignKey("id_command")]
    public Commands? Command { get; set; }
}