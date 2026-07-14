using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;

namespace ElectrostoreAPI.Models;

public class Projets : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_projet { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public required string nom_projet { get; set; }

    [MaxLength(Constants.MaxDescriptionLength)]
    public string description_projet { get; set; } = string.Empty;

    [MaxLength(Constants.MaxUrlLength)]
    public string url_projet { get; set; } = string.Empty;

    public ProjetStatus status_projet { get; set; } = ProjetStatus.NotStarted;

    public ICollection<ProjetsCommentaires> ProjetsCommentaires { get; set; } = new List<ProjetsCommentaires>();
    public ICollection<ProjetsDocuments> ProjetsDocuments { get; set; } = new List<ProjetsDocuments>();
    public ICollection<ProjetsItems> ProjetsItems { get; set; } = new List<ProjetsItems>();
    public ICollection<ProjetsProjetTags> ProjetsProjetTags { get; set; } = new List<ProjetsProjetTags>();
    public ICollection<ProjetsStatus> ProjetsStatus { get; set; } = new List<ProjetsStatus>();
}