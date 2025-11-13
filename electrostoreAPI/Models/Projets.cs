using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using electrostore.Dto;
using electrostore.Enums;

namespace electrostore.Models;

public class Projets : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_projet { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public required string nom_projet { get; set; }

    [MaxLength(Constants.MaxDescriptionLength)]
    public required string description_projet { get; set; }

    [MaxLength(Constants.MaxUrlLength)]
    public required string url_projet { get; set; }

    public ProjetStatus status_projet { get; set; } = ProjetStatus.NotStarted;

    public ICollection<ProjetsCommentaires> ProjetsCommentaires { get; set; } = new List<ProjetsCommentaires>();
    public ICollection<ProjetsDocuments> ProjetsDocuments { get; set; } = new List<ProjetsDocuments>();
    public ICollection<ProjetsItems> ProjetsItems { get; set; } = new List<ProjetsItems>();
    public ICollection<ProjetsProjetTags> ProjetsProjetTags { get; set; } = new List<ProjetsProjetTags>();
    public ICollection<ProjetsStatus> ProjetsStatus { get; set; } = new List<ProjetsStatus>();
}