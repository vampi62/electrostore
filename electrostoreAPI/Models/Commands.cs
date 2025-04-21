using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using electrostore.Dto;

namespace electrostore.Models;

public class Commands : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_command { get; set; }

    public float prix_command { get; set; }
    
    [MaxLength(Constants.MaxUrlLength)]
    public string url_command { get; set; }

    [MaxLength(Constants.MaxStatusLength)]
    public string status_command { get; set; }

    public DateTime date_command { get; set; }

    public DateTime? date_livraison_command { get; set; }

    public ICollection<CommandsItems> CommandsItems { get; set; } = new List<CommandsItems>();
    public ICollection<CommandsCommentaires> CommandsCommentaires { get; set; } = new List<CommandsCommentaires>();
    public ICollection<CommandsDocuments> CommandsDocuments { get; set; } = new List<CommandsDocuments>();
}