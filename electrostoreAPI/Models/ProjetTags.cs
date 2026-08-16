using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Models;

public class ProjetTags : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int id_project_tag { get; set; }

    [MaxLength(Constants.MaxNameLength)]
    public required string name_project_tag { get; set; }

    public int weight_project_tag { get; set; } = 0;

    public ICollection<ProjetsProjetTags> ProjetsProjetTags { get; set; } = new List<ProjetsProjetTags>();
}