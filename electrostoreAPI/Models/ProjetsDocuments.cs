using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using electrostore.Dto;
using System.Numerics;

namespace electrostore.Models;

public class ProjetsDocuments : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_projet_document { get; set; }

    [MaxLength(Constants.MaxUrlLength)]
    public required string url_projet_document { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public required string name_projet_document { get; set; }

    [MaxLength(Constants.MaxTypeLength)]
    public required string type_projet_document { get; set; }

    public decimal size_projet_document { get; set; }

    public int id_projet { get; set; }

    [ForeignKey("id_projet")]
    public Projets Projet { get; set; }
}