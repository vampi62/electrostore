using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;

namespace ElectrostoreAPI.Models;

public class Projets : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_project { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public required string name_project { get; set; }

    [MaxLength(Constants.MaxDescriptionLength)]
    public string description_project { get; set; } = string.Empty;

    [MaxLength(Constants.MaxUrlLength)]
    public string url_project { get; set; } = string.Empty;

    public ProjetStatus status_project { get; set; } = ProjetStatus.NotStarted;

    public ICollection<ProjetsCommentaires> ProjetsCommentaires { get; set; } = new List<ProjetsCommentaires>();
    public ICollection<ProjetsDocuments> ProjetsDocuments { get; set; } = new List<ProjetsDocuments>();
    public ICollection<ProjetsItems> ProjetsItems { get; set; } = new List<ProjetsItems>();
    public ICollection<ProjetsProjetTags> ProjetsProjetTags { get; set; } = new List<ProjetsProjetTags>();
    public ICollection<ProjetsStatus> ProjetsStatus { get; set; } = new List<ProjetsStatus>();
}